using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GGo_SIM_v1
{
    public static class SmartNumberFormat
    {
        public static string Format(decimal value, int sigDigits = 6)
        {
            if (sigDigits < 1) sigDigits = 1;

            // 0 처리
            if (value == 0) return "0";

            decimal abs = Math.Abs(value);

            // 1) |x| < 1 : 0.xxxxxx (반올림)
            if (abs < 1m)
            {
                var rounded = Math.Round(value, sigDigits, MidpointRounding.AwayFromZero);
                return rounded.ToString("0." + new string('0', sigDigits), CultureInfo.InvariantCulture);
            }

            // 2) 단위 축약 (k / M)
            string suffix = "";
            decimal scaled = value;
            if (abs >= 1_000_000_000m)
            {
                scaled = value / 1_000_000_000m;
                suffix = "억";
            }
            if (abs >= 1_000_000m)
            {
                scaled = value / 10_000m;
                suffix = "만";
            }

            // 3) 소수부를 잘라서(절삭) 총 자릿수 ~6 맞추기
            int intDigits = CountIntegerDigits(scaled);
            int decimalsToKeep = Math.Max(0, sigDigits - intDigits); // 정수부가 6자리 넘으면 소수 0자리

            decimal truncated = TruncateDecimal(scaled, decimalsToKeep);

            // 소수부는 최대 decimalsToKeep까지 표시(불필요한 0은 생략)
            string fmt = decimalsToKeep > 0 ? ("0." + new string('#', decimalsToKeep)) : "0";
            return truncated.ToString(fmt, CultureInfo.InvariantCulture) + suffix;
        }

        // 정수부 자리수 계산
        private static int CountIntegerDigits(decimal v)
        {
            v = Math.Abs(decimal.Truncate(v));
            if (v == 0) return 1;
            int count = 0;
            while (v >= 1)
            {
                v = decimal.Truncate(v / 10m);
                count++;
            }
            return count;
        }

        // 소수부 절삭
        private static decimal TruncateDecimal(decimal v, int decimals)
        {
            if (decimals <= 0) return decimal.Truncate(v);
            decimal factor = Pow10(decimals);
            return decimal.Truncate(v * factor) / factor;
        }

        private static decimal Pow10(int n)
        {
            decimal f = 1m;
            for (int i = 0; i < n; i++) f *= 10m;
            return f;
        }
    }
}
