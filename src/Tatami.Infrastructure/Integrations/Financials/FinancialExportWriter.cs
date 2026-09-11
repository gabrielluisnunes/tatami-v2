using ClosedXML.Excel;
using Tatami.Application.Financials;

namespace Tatami.Infrastructure.Integrations.Financials;

public class FinancialExportWriter : IFinancialExportWriter
{
    public byte[] WriteOverdue(string month, IReadOnlyList<FinancialRow> rows)
    {
        ArgumentNullException.ThrowIfNull(month);
        ArgumentNullException.ThrowIfNull(rows);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Inadimplentes");
        sheet.Cell(1, 1).Value = "Mês";
        sheet.Cell(1, 2).Value = month;

        string[] headers = ["Aluno ID", "Aluno", "E-mail", "Ativo", "Dia de vencimento", "Cobrança ID",
            "Valor", "Vencimento", "Competência", "Status", "Pago em"];
        for (var column = 0; column < headers.Length; column++)
        {
            sheet.Cell(3, column + 1).Value = headers[column];
        }

        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            var number = index + 4;
            sheet.Cell(number, 1).Value = row.StudentId.ToString();
            sheet.Cell(number, 2).Value = row.StudentName;
            sheet.Cell(number, 3).Value = row.Email;
            sheet.Cell(number, 4).Value = row.IsActive;
            if (row.PaymentDueDay.HasValue)
            {
                sheet.Cell(number, 5).Value = row.PaymentDueDay.Value;
            }
            if (row.FinancialId.HasValue)
            {
                sheet.Cell(number, 6).Value = row.FinancialId.Value.ToString();
            }
            if (row.Amount.HasValue)
            {
                sheet.Cell(number, 7).Value = row.Amount.Value;
            }
            if (row.DueDate.HasValue)
            {
                sheet.Cell(number, 8).Value = row.DueDate.Value.ToDateTime(TimeOnly.MinValue);
            }
            sheet.Cell(number, 9).Value = row.ReferenceMonth.ToDateTime(TimeOnly.MinValue);
            sheet.Cell(number, 10).Value = row.Status;
            if (row.PaidAt.HasValue)
            {
                sheet.Cell(number, 11).Value = row.PaidAt.Value;
            }
        }

        sheet.Range(3, 1, 3, headers.Length).Style.Font.Bold = true;
        sheet.Column(7).Style.NumberFormat.Format = "#,##0.00";
        sheet.Column(8).Style.DateFormat.Format = "dd/MM/yyyy";
        sheet.Column(9).Style.DateFormat.Format = "MM/yyyy";
        sheet.Column(11).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
        sheet.Columns(1, headers.Length).Width = 22;
        sheet.Column(2).Width = 35;
        sheet.Column(3).Width = 35;
        sheet.SheetView.FreezeRows(3);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}