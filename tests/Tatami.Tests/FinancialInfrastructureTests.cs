using System.Data.Common;
using System.Globalization;
using System.IO.Compression;
using System.Linq.Expressions;
using System.Xml.Linq;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using Tatami.Application.Financials;
using Tatami.Domain.Entities;
using Tatami.Domain.Enums;
using Tatami.Infrastructure.Integrations.Financials;
using Tatami.Infrastructure.Persistence;
using Tatami.Infrastructure.Persistence.Repositories;

namespace Tatami.Tests;

public class FinancialInfrastructureTests
{
    private readonly PixBrCodeGenerator _pix = new();

    [Fact]
    public void PixMatchesIndependentCrcVector()
    {
        // CRC calculado independentemente com binascii.crc_hqx (CCITT-FALSE, início 0xffff).
        const string expected = "00020126580014br.gov.bcb.pix0136123e4567-e12b-12d1-a456-426655440000"
            + "5204000053039865406123.455802BR5913FULANO DE TAL6008BRASILIA62070503***6304488D";

        Assert.Equal(expected, _pix.Build("123e4567-e12b-12d1-a456-426655440000",
            PixKeyType.Aleatoria, "Fulano de Tal", 123.45m, ""));
    }

    [Theory]
    [InlineData(PixKeyType.Celular, "+5511999999999")]
    [InlineData(PixKeyType.Email, "academia@example.com")]
    [InlineData(PixKeyType.Cpf, "12345678901")]
    [InlineData(PixKeyType.Cnpj, "12345678000195")]
    [InlineData(PixKeyType.Aleatoria, "123e4567-e12b-12d1-a456-426655440000")]
    public void PixEncodesAllKeyTypesAndEmvFields(PixKeyType type, string key)
    {
        var fields = ParseEmv(_pix.Build(key, type, "Academia", 0.01m, "Mensalidade"));
        var account = ParseEmv(fields["26"]);

        Assert.Equal("01", fields["00"]);
        Assert.Equal("br.gov.bcb.pix", account["00"]);
        Assert.Equal(key, account["01"]);
        Assert.Equal("MENSALIDADE", account["02"]);
        Assert.Equal("0000", fields["52"]);
        Assert.Equal("986", fields["53"]);
        Assert.Equal("0.01", fields["54"]);
        Assert.Equal("BR", fields["58"]);
        Assert.Equal("ACADEMIA", fields["59"]);
        Assert.Equal("BRASILIA", fields["60"]);
        Assert.Equal("***", ParseEmv(fields["62"])["05"]);
        Assert.Matches("^[0-9A-F]{4}$", fields["63"]);
    }

    [Theory]
    [InlineData(PixKeyType.Celular, "11999999999")]
    [InlineData(PixKeyType.Celular, "+01234567890")]
    [InlineData(PixKeyType.Celular, "+55119999999999999")]
    [InlineData(PixKeyType.Email, "invalid@")]
    [InlineData(PixKeyType.Email, "a b@example.com")]
    [InlineData(PixKeyType.Email, "josé@example.com")]
    [InlineData(PixKeyType.Cpf, "123.456.789-01")]
    [InlineData(PixKeyType.Cpf, "１２３４５６７８９０１")]
    [InlineData(PixKeyType.Cpf, "1234567890")]
    [InlineData(PixKeyType.Cnpj, "12345678901")]
    [InlineData(PixKeyType.Aleatoria, "123e4567e12b12d1a456426655440000")]
    [InlineData(PixKeyType.Aleatoria, "123e4567-e12b-12d1-a456-42665544000z")]
    [InlineData(PixKeyType.Email, "")]
    [InlineData(PixKeyType.Email, " ")]
    [InlineData(PixKeyType.Email, null)]
    [InlineData((PixKeyType)999, "academia@example.com")]
    public void PixRejectsInvalidKeys(PixKeyType type, string? key)
    {
        Assert.ThrowsAny<ArgumentException>(() => _pix.Build(key!, type, "Academia", 10m, ""));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    [InlineData("1.001")]
    [InlineData("10000000000")]
    [InlineData("79228162514264337593543950335")]
    public void PixRejectsInvalidAmounts(string value)
    {
        var amount = decimal.Parse(value, CultureInfo.InvariantCulture);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _pix.Build("12345678901", PixKeyType.Cpf, "Academia", amount, ""));
    }

    [Fact]
    public void PixAmountIsInvariantAndSupportsEmvMaximum()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-BR");
            var fields = ParseEmv(_pix.Build("12345678901", PixKeyType.Cpf, "Academia", 9999999999.99m, ""));
            Assert.Equal("9999999999.99", fields["54"]);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void PixNormalizesAccentsWhitespaceAsciiAndMerchantLength()
    {
        var payload = _pix.Build("12345678901", PixKeyType.Cpf,
            "  São\tJoão  Ação 🥋 " + new string('a', 30), 10m, "  Mensalidade março\n2026  ");
        var fields = ParseEmv(payload);

        Assert.Equal("SAO JOAO ACAO " + new string('A', 11), fields["59"]);
        Assert.Equal(25, fields["59"].Length);
        Assert.Equal("MENSALIDADE MARCO 2026", ParseEmv(fields["26"])["02"]);
        Assert.All(payload, character => Assert.InRange(character, ' ', '~'));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("🥋漢字")]
    public void PixRejectsEmptyNormalizedMerchant(string? merchant)
    {
        Assert.ThrowsAny<ArgumentException>(() => _pix.Build("12345678901", PixKeyType.Cpf, merchant!, 10m, ""));
    }

    [Theory]
    [InlineData(11)]
    [InlineData(72)]
    [InlineData(73)]
    [InlineData(74)]
    [InlineData(77)]
    public void PixTruncatesOnlyDescriptionToFitAccountTemplate(int keyLength)
    {
        var key = new string('a', keyLength - 6) + "@b.com";
        var fields = ParseEmv(_pix.Build(key, PixKeyType.Email, "Academia", 10m, new string('d', 200)));
        var account = ParseEmv(fields["26"]);

        Assert.Equal(key, account["01"]);
        Assert.InRange(fields["26"].Length, 1, 99);
        if (keyLength < 73)
        {
            Assert.Equal(new string('D', 73 - keyLength), account["02"]);
            Assert.Equal(99, fields["26"].Length);
        }
        else
        {
            Assert.False(account.ContainsKey("02"));
        }
    }

    [Fact]
    public void PixRejectsLongKeyInsteadOfSilentlyTruncating()
    {
        var key = new string('a', 60) + "@" + new string('b', 13) + ".com";
        Assert.Equal(78, key.Length);
        Assert.Throws<ArgumentException>(() => _pix.Build(key, PixKeyType.Email, "Academia", 10m, ""));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PixDescriptionIsOptional(string? description)
    {
        var fields = ParseEmv(_pix.Build("12345678901", PixKeyType.Cpf, "Academia", 10m, description!));
        Assert.False(ParseEmv(fields["26"]).ContainsKey("02"));
    }

    [Fact]
    public void ExportPreservesReceivedMonthRowsTypesAndNullsWithoutFiltering()
    {
        var paidAt = new DateTime(2024, 2, 3, 14, 30, 0, DateTimeKind.Utc);
        FinancialRow[] rows =
        [
            new(Guid.NewGuid(), "José", "jose@example.com", true, 31, Guid.NewGuid(), 123.45m,
                new DateOnly(2024, 2, 29), new DateOnly(2024, 2, 1), "overdue", paidAt),
            new(Guid.NewGuid(), "Ana", "ana@example.com", false, null, null, null,
                null, new DateOnly(2023, 12, 1), "paid", null),
        ];

        using var stream = new MemoryStream(new FinancialExportWriter().WriteOverdue("2024-02", rows));
        using var workbook = new XLWorkbook(stream);
        var sheet = Assert.Single(workbook.Worksheets);

        Assert.Equal("2024-02", sheet.Cell("B1").GetString());
        Assert.Equal(5, sheet.LastRowUsed()!.RowNumber());
        Assert.Equal(rows[0].StudentId.ToString(), sheet.Cell("A4").GetString());
        Assert.Equal("José", sheet.Cell("B4").GetString());
        Assert.Equal("jose@example.com", sheet.Cell("C4").GetString());
        Assert.True(sheet.Cell("D4").GetBoolean());
        Assert.Equal(31, sheet.Cell("E4").GetDouble());
        Assert.Equal(rows[0].FinancialId.ToString(), sheet.Cell("F4").GetString());
        Assert.Equal(123.45m, sheet.Cell("G4").GetValue<decimal>());
        Assert.Equal(XLDataType.Number, sheet.Cell("G4").DataType);
        Assert.Equal(new DateTime(2024, 2, 29), sheet.Cell("H4").GetDateTime());
        Assert.Equal(new DateTime(2024, 2, 1), sheet.Cell("I4").GetDateTime());
        Assert.Equal("overdue", sheet.Cell("J4").GetString());
        Assert.Equal(paidAt, sheet.Cell("K4").GetDateTime());
        Assert.Equal("Ana", sheet.Cell("B5").GetString());
        Assert.False(sheet.Cell("D5").GetBoolean());
        Assert.All(sheet.Range("E5:H5").Cells(), cell => Assert.True(cell.IsEmpty()));
        Assert.Equal(new DateTime(2023, 12, 1), sheet.Cell("I5").GetDateTime());
        Assert.Equal("paid", sheet.Cell("J5").GetString());
        Assert.True(sheet.Cell("K5").IsEmpty());
    }

    [Theory]
    [InlineData("=HYPERLINK(\"https://example.com\",\"x\")")]
    [InlineData("+SUM(1,2)")]
    [InlineData("-1+2")]
    [InlineData("@SUM(1,2)")]
    [InlineData("\t=1+1")]
    public void ExportNeverTurnsUntrustedTextIntoFormulas(string text)
    {
        var row = new FinancialRow(Guid.NewGuid(), text, text, true, null, null, null,
            null, new DateOnly(2024, 1, 1), text, null);
        var bytes = new FinancialExportWriter().WriteOverdue(text, [row]);
        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet(1);
        foreach (var address in new[] { "B1", "B4", "C4", "J4" })
        {
            Assert.Equal(text, sheet.Cell(address).GetString());
            Assert.Equal(XLDataType.Text, sheet.Cell(address).DataType);
            Assert.False(sheet.Cell(address).HasFormula);
        }

        using var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        using var xml = archive.GetEntry("xl/worksheets/sheet1.xml")!.Open();
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        Assert.Empty(XDocument.Load(xml).Descendants(ns + "f"));
    }

    [Fact]
    public void ExportEmptyRowsStillIncludesReceivedMonthAndHeaders()
    {
        using var stream = new MemoryStream(new FinancialExportWriter().WriteOverdue("2000-01", []));
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet(1);
        Assert.Equal("2000-01", sheet.Cell("B1").GetString());
        Assert.Equal(3, sheet.LastRowUsed()!.RowNumber());
        Assert.Equal(11, sheet.Row(3).CellsUsed().Count());
    }

    [Fact]
    public void FinancialModelHasExpectedPostgresUniqueIndexAndPrecision()
    {
        using var context = new TatamiDbContext(Options());
        var entity = context.Model.FindEntityType(typeof(Financial))!;
        var index = Assert.Single(entity.GetIndexes(), item =>
            item.GetDatabaseName() == "IX_financials_StudentId_ReferenceMonth");
        Assert.True(index.IsUnique);
        Assert.Equal(new[] { "StudentId", "ReferenceMonth" }, index.Properties.Select(property => property.Name));
        Assert.Equal(10, entity.FindProperty(nameof(Financial.Amount))!.GetPrecision());
        Assert.Equal(2, entity.FindProperty(nameof(Financial.Amount))!.GetScale());
        Assert.Equal("aguardando_confirmacao", entity.FindProperty(nameof(Financial.Status))!
            .GetTypeMapping().Converter!.ConvertToProvider(FinancialStatus.AguardandoConfirmacao));
    }

    [Fact]
    public async Task TryCreateDetachesOnlyDuplicateEntityAndReturnsFalse()
    {
        var contextMock = new Mock<TatamiDbContext>(Options()) { CallBase = true };
        using var context = contextMock.Object;
        contextMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(UpdateException(PostgresErrorCodes.UniqueViolation, "IX_financials_StudentId_ReferenceMonth"));
        var financial = new Financial { Id = Guid.NewGuid() };
        var other = new Financial { Id = Guid.NewGuid() };
        context.Financials.Attach(other);

        Assert.False(await new FinancialRepository(context).TryCreateAsync(financial));
        Assert.Equal(EntityState.Detached, context.Entry(financial).State);
        Assert.Equal(EntityState.Unchanged, context.Entry(other).State);
    }

    [Theory]
    [InlineData(PostgresErrorCodes.UniqueViolation, "PK_financials")]
    [InlineData(PostgresErrorCodes.UniqueViolation, "other_index")]
    [InlineData(PostgresErrorCodes.CheckViolation, "IX_financials_StudentId_ReferenceMonth")]
    [InlineData(PostgresErrorCodes.ForeignKeyViolation, "FK_financials_students_StudentId")]
    public async Task TryCreatePropagatesOtherDatabaseErrors(string code, string constraint)
    {
        var contextMock = new Mock<TatamiDbContext>(Options()) { CallBase = true };
        using var context = contextMock.Object;
        var exception = UpdateException(code, constraint);
        contextMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(exception);

        Assert.Same(exception, await Assert.ThrowsAsync<DbUpdateException>(() =>
            new FinancialRepository(context).TryCreateAsync(new Financial { Id = Guid.NewGuid() })));
    }

    [Fact]
    public async Task TryCreateReturnsTrueAndPassesCancellationTokenOnSuccess()
    {
        var contextMock = new Mock<TatamiDbContext>(Options()) { CallBase = true };
        using var context = contextMock.Object;
        using var cancellation = new CancellationTokenSource();
        contextMock.Setup(db => db.SaveChangesAsync(cancellation.Token)).ReturnsAsync(1);

        Assert.True(await new FinancialRepository(context).TryCreateAsync(new Financial { Id = Guid.NewGuid() }, cancellation.Token));
        contextMock.Verify(db => db.SaveChangesAsync(cancellation.Token), Times.Once);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 1)]
    [InlineData(false, 0)]
    public async Task TrySetStatusGeneratesAtomicTenantScopedPostgresUpdate(bool replaceAmount, int affected)
    {
        var capture = new UpdateCommandCapture(affected);
        using var context = new TatamiDbContext(Options(new SuppressConnection(), capture));
        var academyId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var now = new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);

        var result = await new FinancialRepository(context).TrySetStatusAsync(academyId, id,
            FinancialStatus.AguardandoConfirmacao, FinancialStatus.Paid, replaceAmount ? now : null,
            replaceAmount ? 123.45m : null, now);

        Assert.Equal(affected == 1, result);
        Assert.StartsWith("UPDATE financials", capture.Sql);
        var where = capture.Sql[capture.Sql.IndexOf("WHERE", StringComparison.Ordinal)..];
        Assert.Contains("\"AcademyId\" =", where);
        Assert.Contains("\"Id\" =", where);
        Assert.Contains("\"Status\" =", where);
        Assert.Contains(academyId, capture.Values);
        Assert.Contains(id, capture.Values);
        Assert.Contains("aguardando_confirmacao", capture.Values);
        Assert.Contains("paid", capture.Values);
        Assert.Contains("\"PaidAt\" =", capture.Sql);
        Assert.Contains("\"UpdatedAt\" =", capture.Sql);
        Assert.Contains(now, capture.Values);
        Assert.Equal(replaceAmount, capture.Sql.Contains("\"Amount\" =", StringComparison.Ordinal));
        if (replaceAmount)
        {
            Assert.Contains(123.45m, capture.Values);
        }
        else
        {
            Assert.Contains("\"PaidAt\" = NULL", capture.Sql);
        }
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Theory]
    [InlineData("month")]
    [InlineData("student")]
    [InlineData("id")]
    [InlineData("studentMonth")]
    [InlineData("candidates")]
    [InlineData("pending")]
    public async Task QueriesGenerateExpectedPostgresFiltersWithoutTracking(string query)
    {
        var capture = new QueryCommandCapture();
        using var context = new TatamiDbContext(Options(new SuppressConnection(), capture));
        var repository = new FinancialRepository(context);
        var academyId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var date = new DateOnly(2026, 9, 1);

        await Assert.ThrowsAsync<QueryCapturedException>(async () =>
        {
            switch (query)
            {
                case "month": await repository.ListMonthAsync(academyId, date); break;
                case "student": await repository.ListStudentAsync(academyId, studentId); break;
                case "id": await repository.GetAsync(academyId, id); break;
                case "studentMonth": await repository.GetStudentMonthAsync(academyId, studentId, date); break;
                case "candidates": await repository.ListChargeCandidatesAsync(); break;
                case "pending": await repository.ListPendingBeforeAsync(date); break;
            }
        });

        Assert.Contains("AsNoTracking()", capture.Expression);
        Assert.StartsWith("SELECT", capture.Sql);
        var where = capture.Sql[capture.Sql.IndexOf("WHERE", StringComparison.Ordinal)..];
        if (query == "candidates")
        {
            Assert.Contains("INNER JOIN academies", capture.Sql);
            Assert.Contains("\"IsActive\"", where);
            Assert.Contains("\"PaymentDueDay\" >= 1", where);
            Assert.Contains("\"PaymentDueDay\" <= 31", where);
            Assert.Contains("\"MonthlyPrice\" > 0", where);
            Assert.DoesNotContain("CURRENT_DATE", capture.Sql);
            Assert.Empty(capture.Values);
        }
        else if (query == "pending")
        {
            Assert.Contains("\"Status\" = 'pending'", where);
            Assert.Contains("\"DueDate\" <", where);
            Assert.Contains(date, capture.Values);
        }
        else
        {
            Assert.Contains("\"AcademyId\" =", where);
            Assert.Contains(academyId, capture.Values);
            if (query is "student" or "studentMonth")
            {
                Assert.Contains("\"StudentId\" =", where);
                Assert.Contains(studentId, capture.Values);
            }
            if (query is "month" or "studentMonth")
            {
                Assert.Contains("\"ReferenceMonth\" =", where);
                Assert.Contains(date, capture.Values);
            }
            if (query == "id")
            {
                Assert.Contains("\"Id\" =", where);
                Assert.Contains(id, capture.Values);
            }
        }
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task NotificationStubLogsOnlyGenericMessages()
    {
        var logger = new Mock<ILogger<StubFinancialNotificationSender>>();
        var sender = new StubFinancialNotificationSender(logger.Object);
        var financial = new Financial { Id = Guid.NewGuid(), StudentId = Guid.NewGuid(), Amount = 123.45m };

        await sender.SendDueTodayAsync(financial);
        await sender.SendOverdueAsync(financial);

        var calls = logger.Invocations.Where(call => call.Method.Name == "Log").ToList();
        Assert.Equal(2, calls.Count);
        Assert.Equal("Financial due-today notification stub. Notification not sent yet.", calls[0].Arguments[2].ToString());
        Assert.Equal("Financial overdue notification stub. Notification not sent yet.", calls[1].Arguments[2].ToString());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sender.SendDueTodayAsync(financial, cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sender.SendOverdueAsync(financial, cancellation.Token));
    }

    private static Dictionary<string, string> ParseEmv(string payload)
    {
        var fields = new Dictionary<string, string>();
        var offset = 0;
        while (offset < payload.Length)
        {
            Assert.True(payload.Length - offset >= 4);
            var id = payload.Substring(offset, 2);
            var length = int.Parse(payload.Substring(offset + 2, 2), CultureInfo.InvariantCulture);
            offset += 4;
            Assert.InRange(length, 1, 99);
            Assert.True(offset + length <= payload.Length);
            Assert.True(fields.TryAdd(id, payload.Substring(offset, length)));
            offset += length;
        }
        Assert.Equal(payload.Length, offset);
        return fields;
    }

    private static DbContextOptions<TatamiDbContext> Options(params IInterceptor[] interceptors) =>
        new DbContextOptionsBuilder<TatamiDbContext>()
            .UseNpgsql("Host=localhost;Database=financial_sql_tests;Username=unused;Password=unused")
            .AddInterceptors(interceptors).Options;

    private static DbUpdateException UpdateException(string code, string constraint) =>
        new("Falha de gravação", new PostgresException("Falha", "ERROR", "ERROR", code, constraintName: constraint));

    private sealed class SuppressConnection : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection,
            ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(InterceptionResult.Suppress());
    }

    private sealed class UpdateCommandCapture(int affected) : DbCommandInterceptor
    {
        public string Sql { get; private set; } = string.Empty;
        public object?[] Values { get; private set; } = [];

        // Valida tradução SQL do provider real, sem simular concorrência de um servidor Postgres.
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Sql = command.CommandText;
            Values = command.Parameters.Cast<DbParameter>().Select(parameter => parameter.Value).ToArray();
            return ValueTask.FromResult(InterceptionResult<int>.SuppressWithResult(affected));
        }
    }

    private sealed class QueryCapturedException : Exception;

    private sealed class QueryCommandCapture : DbCommandInterceptor, IQueryExpressionInterceptor
    {
        public string Sql { get; private set; } = string.Empty;
        public string Expression { get; private set; } = string.Empty;
        public object?[] Values { get; private set; } = [];

        public Expression QueryCompilationStarting(Expression queryExpression, QueryExpressionEventData eventData)
        {
            Expression = queryExpression.ToString();
            return queryExpression;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Sql = command.CommandText;
            Values = command.Parameters.Cast<DbParameter>().Select(parameter => parameter.Value).ToArray();
            throw new QueryCapturedException();
        }
    }
}