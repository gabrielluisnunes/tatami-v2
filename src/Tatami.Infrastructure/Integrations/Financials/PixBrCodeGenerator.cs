using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Tatami.Application.Financials;
using Tatami.Domain.Enums;

namespace Tatami.Infrastructure.Integrations.Financials;

public class PixBrCodeGenerator : IPixBrCodeGenerator
{
    public string Build(string pixKey, PixKeyType keyType, string merchantName, decimal amount, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pixKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(merchantName);
        pixKey = pixKey.Trim();

        var pattern = keyType switch
        {
            PixKeyType.Celular => @"\A\+[1-9][0-9]{9,14}\z",
            PixKeyType.Email => @"\A[^\s@]+@[^\s@]+\.[^\s@]+\z",
            PixKeyType.Cpf => @"\A[0-9]{11}\z",
            PixKeyType.Cnpj => @"\A[0-9]{14}\z",
            PixKeyType.Aleatoria => @"\A[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\z",
            _ => throw new ArgumentOutOfRangeException(nameof(keyType), "Tipo de chave PIX inválido."),
        };

        if (pixKey.Length > 77 || pixKey.Any(character => character is < '!' or > '~')
            || !Regex.IsMatch(pixKey, pattern, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)))
        {
            throw new ArgumentException("Chave PIX inválida ou excede o limite EMV de 77 caracteres.", nameof(pixKey));
        }

        if (amount <= 0 || amount > 9999999999.99m || decimal.Round(amount, 2) != amount)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Valor PIX deve ser positivo, com até duas casas decimais e 13 caracteres.");
        }

        var merchant = NormalizeText(merchantName);
        if (merchant.Length == 0)
        {
            throw new ArgumentException("Nome do recebedor deve conter caracteres ASCII válidos.", nameof(merchantName));
        }

        merchant = merchant[..Math.Min(25, merchant.Length)].TrimEnd();
        var account = Field("00", "br.gov.bcb.pix") + Field("01", pixKey);
        var detail = NormalizeText(description ?? string.Empty);
        var available = 99 - account.Length - 4;
        if (detail.Length > 0 && available > 0)
        {
            account += Field("02", detail[..Math.Min(available, detail.Length)]);
        }

        var payload = Field("00", "01")
            + Field("26", account)
            + Field("52", "0000")
            + Field("53", "986")
            + Field("54", amount.ToString("F2", CultureInfo.InvariantCulture))
            + Field("58", "BR")
            + Field("59", merchant)
            + Field("60", "BRASILIA")
            + Field("62", Field("05", "***"))
            + "6304";

        return payload + Crc16(payload).ToString("X4", CultureInfo.InvariantCulture);
    }

    private static string NormalizeText(string value)
    {
        var ascii = new StringBuilder();
        foreach (var character in value.Normalize(NormalizationForm.FormD).ToUpperInvariant())
        {
            if (character is >= ' ' and <= '~')
            {
                ascii.Append(character);
            }
            else if (char.IsWhiteSpace(character))
            {
                ascii.Append(' ');
            }
        }

        return Regex.Replace(ascii.ToString(), " +", " ").Trim();
    }

    private static string Field(string id, string value)
    {
        if (value.Length > 99)
        {
            throw new ArgumentException("Campo excede o limite EMV de 99 caracteres.", nameof(value));
        }

        return id + value.Length.ToString("D2", CultureInfo.InvariantCulture) + value;
    }

    private static ushort Crc16(string payload)
    {
        var crc = 0xffff;
        foreach (var character in payload)
        {
            crc ^= character << 8;
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 0x8000) != 0 ? ((crc << 1) ^ 0x1021) & 0xffff : (crc << 1) & 0xffff;
            }
        }

        return (ushort)crc;
    }
}