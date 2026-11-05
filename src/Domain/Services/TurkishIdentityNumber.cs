namespace Domain.Services;

/// <summary>
/// T.C. kimlik numarası: isteğe bağlı bir alan, ama girildiyse gerçek bir numara olmalı.
///
/// Kural 11 hane, ilk hane 0 değil, 10. hane (tek konumlar×7 − çift konumlar) mod 10, 11. hane
/// ilk on hanenin toplamı mod 10. Yazım hatasını yakalamaya yeter; numaranın gerçekten o kişiye ait
/// olduğunu (NVİ doğrulaması) söylemez.
/// </summary>
public static class TurkishIdentityNumber
{
    /// <summary>Boşlukları atar; boş girişi null yapar, böylece "alan yok" tek biçimde saklanır.</summary>
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray());
    }

    public static bool IsValid(string? value)
    {
        if (value is not { Length: 11 } || value[0] == '0' || !value.All(c => c is >= '0' and <= '9'))
            return false;

        var d = value.Select(c => c - '0').ToArray();
        var odd = d[0] + d[2] + d[4] + d[6] + d[8];
        var even = d[1] + d[3] + d[5] + d[7];
        var tenth = ((odd * 7 - even) % 10 + 10) % 10;
        if (tenth != d[9])
            return false;

        return d.Take(10).Sum() % 10 == d[10];
    }
}
