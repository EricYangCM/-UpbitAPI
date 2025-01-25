using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace GGo_v1.UpbitAPI
{
    public class Upbit_Account
    {
        private readonly string _accessKey;
        private readonly string _secretKey;

        public Upbit_Account(string accessKey, string secretKey)
        {
            _accessKey = accessKey;
            _secretKey = secretKey;
        }


        #region Data Class


        // 계좌 정보 클래스
        public class Account
        {
            [JsonProperty("currency")]
            public string Currency { get; set; }

            [JsonProperty("balance")]
            public string Balance { get; set; }

            [JsonProperty("locked")]
            public string Locked { get; set; }

            [JsonProperty("avg_buy_price")]
            public string AvgBuyPrice { get; set; }

            [JsonProperty("avg_buy_price_modified")]
            public bool AvgBuyPriceModified { get; set; }

            [JsonProperty("unit_currency")]
            public string UnitCurrency { get; set; }
        }


        // 코인 정보 클래스
        public class CoinName
        {
            [JsonProperty("market")]
            public string Name_Market { get; set; }

            [JsonProperty("korean_name")]
            public string Name_KR { get; set; }

            [JsonProperty("english_name")]
            public string Name_ENG { get; set; }

            [JsonProperty("market_event.warning")]
            public bool Warning { get; set; }

            [JsonProperty("market_event.caution")]
            public CautionDetails Caution { get; set; } // Caution 필드는 별도 클래스 정의
        }

        // 코인 정보 내 주의 세부정보
        public class CautionDetails
        {
            [JsonProperty("PRICE_FLUCTUATIONS")]
            public bool PriceFluctuations { get; set; }

            [JsonProperty("TRADING_VOLUME_SOARING")]
            public bool TradingVolumeSoaring { get; set; }

            [JsonProperty("DEPOSIT_AMOUNT_SOARING")]
            public bool DepositAmountSoaring { get; set; }

            [JsonProperty("GLOBAL_PRICE_DIFFERENCES")]
            public bool GlobalPriceDifferences { get; set; }

            [JsonProperty("CONCENTRATION_OF_SMALL_ACCOUNTS")]
            public bool ConcentrationOfSmallAccounts { get; set; }
        }


        // 코인 시세 정보 클래스
        public class TickerData
        {
            [JsonProperty("market")]
            public string Market { get; set; } // 종목 구분 코드

            [JsonProperty("trade_date")]
            public string TradeDateUTC { get; set; } // 최근 거래 일자 (UTC)

            [JsonProperty("trade_time")]
            public string TradeTimeUTC { get; set; } // 최근 거래 시간 (UTC)

            [JsonProperty("trade_date_kst")]
            public string TradeDateKST { get; set; } // 최근 거래 일자 (KST)

            [JsonProperty("trade_time_kst")]
            public string TradeTimeKST { get; set; } // 최근 거래 시간 (KST)

            [JsonProperty("trade_timestamp")]
            public long TradeTimestamp { get; set; } // 최근 거래 일시 (UTC, Unix Timestamp)

            [JsonProperty("opening_price")]
            public double OpeningPrice { get; set; } // 시가

            [JsonProperty("high_price")]
            public double HighPrice { get; set; } // 고가

            [JsonProperty("low_price")]
            public double LowPrice { get; set; } // 저가

            [JsonProperty("trade_price")]
            public double TradePrice { get; set; } // 현재가

            [JsonProperty("prev_closing_price")]
            public double PrevClosingPrice { get; set; } // 전일 종가

            [JsonProperty("change")]
            public string Change { get; set; } // 변동 상태 (EVEN, RISE, FALL)

            [JsonProperty("change_price")]
            public double ChangePrice { get; set; } // 변동액의 절대값

            [JsonProperty("change_rate")]
            public double ChangeRate { get; set; } // 변동률의 절대값

            [JsonProperty("signed_change_price")]
            public double SignedChangePrice { get; set; } // 부호가 있는 변동액

            [JsonProperty("signed_change_rate")]
            public double SignedChangeRate { get; set; } // 부호가 있는 변동률

            [JsonProperty("trade_volume")]
            public double TradeVolume { get; set; } // 가장 최근 거래량

            [JsonProperty("acc_trade_price")]
            public double AccTradePrice { get; set; } // 누적 거래대금 (UTC 0시 기준)

            [JsonProperty("acc_trade_price_24h")]
            public double AccTradePrice24h { get; set; } // 24시간 누적 거래대금

            [JsonProperty("acc_trade_volume")]
            public double AccTradeVolume { get; set; } // 누적 거래량 (UTC 0시 기준)

            [JsonProperty("acc_trade_volume_24h")]
            public double AccTradeVolume24h { get; set; } // 24시간 누적 거래량

            [JsonProperty("highest_52_week_price")]
            public double Highest52WeekPrice { get; set; } // 52주 신고가

            [JsonProperty("highest_52_week_date")]
            public string Highest52WeekDate { get; set; } // 52주 신고가 달성일

            [JsonProperty("lowest_52_week_price")]
            public double Lowest52WeekPrice { get; set; } // 52주 신저가

            [JsonProperty("lowest_52_week_date")]
            public string Lowest52WeekDate { get; set; } // 52주 신저가 달성일

            [JsonProperty("timestamp")]
            public long Timestamp { get; set; } // 타임스탬프
        }


        #endregion


        #region Authorization Token


        // Authorization Token 생성
        public string Get_AuthorizationToken(Dictionary<string, string> queryParams = null)
        {
            string tempQueryHash = "";

            // Query Parameter를 SHA512로 해싱
            if (queryParams != null && queryParams.Count > 0)
            {
                // Query Parameter를 정렬하여 문자열 생성 (key=value&key2=value2)
                var sortedQuery = string.Join("&", queryParams
                    .OrderBy(p => p.Key) // 키 정렬
                    .Select(p => $"{p.Key}={p.Value}"));

                tempQueryHash = GetSha512Hash(sortedQuery); // 해싱
            }

            var payload = new JwtPayload
    {
        { "access_key", _accessKey },
        { "nonce", Guid.NewGuid().ToString() },
        { "query_hash", tempQueryHash },
        { "query_hash_alg", "SHA512" }
    };

            byte[] keyBytes = Encoding.Default.GetBytes(_secretKey);
            var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(keyBytes);
            var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(securityKey, "HS256");
            var header = new JwtHeader(credentials);
            var secToken = new JwtSecurityToken(header, payload);
            var jwtToken = new JwtSecurityTokenHandler().WriteToken(secToken);
            return "Bearer " + jwtToken;
        }


        // Query Hash 계산 함수
        private static string GetSha512Hash(string input)
        {
            using (SHA512 sha512 = SHA512.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(input);
                byte[] hash = sha512.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        #endregion



        #region Public Methods



        // 전체 계좌 조회
        public async Task<List<Account>> Get_Accounts()
        {
            string authorizationToken = Get_AuthorizationToken();

            var client = new RestClient("https://api.upbit.com/v1/accounts");
            var request = new RestRequest();
            request.Method = Method.Get;
            request.AddHeader("Authorization", authorizationToken);

            var response = await client.ExecuteAsync(request);

            if (response.IsSuccessful)
            {
                return JsonConvert.DeserializeObject<List<Account>>(response.Content);
            }
            else
            {
                Console.WriteLine($"API 요청 실패: {response.StatusCode} - {response.Content}");
                return null;
            }
        }



        // 전체 코인 리스트 가져오기
        public async Task<List<CoinName>> Get_CoinList()
        {
            // Query Parameter
            var queryParams = new Dictionary<string, string>
            {
                { "is_details", "true" }
            };

            string authorizationToken = Get_AuthorizationToken(queryParams);

            var client = new RestClient("https://api.upbit.com/v1/market/all");
            var request = new RestRequest();
            request.Method = Method.Get;
            request.AddHeader("Authorization", authorizationToken);

            var response = await client.ExecuteAsync(request);

            if (response.IsSuccessful)
            {
                // JSON 데이터를 List<CoinName> 형식으로 역직렬화
                var allCoins = JsonConvert.DeserializeObject<List<CoinName>>(response.Content);

                // KRW-로 시작하는 코인만 필터링, KRW- 제거, Name_KR 기준으로 정렬
                var filteredCoins = allCoins
                    .Where(coin => coin.Name_Market.StartsWith("KRW-")) // KRW-로 시작하는 항목만 필터링
                    .Select(coin => new CoinName
                    {
                        Name_Market = coin.Name_Market.Replace("KRW-", ""), // KRW- 제거
                        Name_KR = coin.Name_KR,
                        Name_ENG = coin.Name_ENG,
                        Warning = coin.Warning,
                        Caution = coin.Caution
                    })
                    .OrderBy(coin => coin.Name_KR) // Name_KR 기준으로 정렬
                    .ToList();

                return filteredCoins;
            }
            else
            {
                Console.WriteLine($"API 요청 실패: {response.StatusCode} - {response.Content}");
                return null;
            }
        }



        // 전체 코인 현재 시세 조회 메서드
        public async Task<List<TickerData>> Get_All_Tickers()
        {
            string authorizationToken = Get_AuthorizationToken();

            var client = new RestClient("https://api.upbit.com/v1/ticker/all?quote_currencies=KRW");
            var request = new RestRequest();
            request.Method = Method.Get;
            request.AddHeader("Authorization", authorizationToken);

            var response = await client.ExecuteAsync(request);

            if (response.IsSuccessful)
            {
                // JSON 데이터를 List<TickerData> 형식으로 역직렬화
                var allTickers = JsonConvert.DeserializeObject<List<TickerData>>(response.Content);

                // KRW-로 시작하는 코인만 필터링
                var filteredAllTickers = allTickers
                    .Where(ticker => ticker.Market.StartsWith("KRW-")) // KRW-로 시작하는 항목만 필터링
                    .Select(ticker => new TickerData
                    {
                        Market = ticker.Market.Replace("KRW-", ""), // KRW- 제거
                        TradeDateUTC = ticker.TradeDateUTC,
                        TradeTimeUTC = ticker.TradeTimeUTC,
                        TradeDateKST = ticker.TradeDateKST,
                        TradeTimeKST = ticker.TradeTimeKST,
                        TradeTimestamp = ticker.TradeTimestamp,
                        OpeningPrice = ticker.OpeningPrice,
                        HighPrice = ticker.HighPrice,
                        LowPrice = ticker.LowPrice,
                        TradePrice = ticker.TradePrice,
                        PrevClosingPrice = ticker.PrevClosingPrice,
                        Change = ticker.Change,
                        ChangePrice = ticker.ChangePrice,
                        ChangeRate = ticker.ChangeRate,
                        SignedChangePrice = ticker.SignedChangePrice,
                        SignedChangeRate = ticker.SignedChangeRate,
                        TradeVolume = ticker.TradeVolume,
                        AccTradePrice = ticker.AccTradePrice,
                        AccTradePrice24h = ticker.AccTradePrice24h,
                        AccTradeVolume = ticker.AccTradeVolume,
                        AccTradeVolume24h = ticker.AccTradeVolume24h,
                        Highest52WeekPrice = ticker.Highest52WeekPrice,
                        Highest52WeekDate = ticker.Highest52WeekDate,
                        Lowest52WeekPrice = ticker.Lowest52WeekPrice,
                        Lowest52WeekDate = ticker.Lowest52WeekDate,
                        Timestamp = ticker.Timestamp
                    })
                    .OrderBy(ticker => ticker.Market) // Market 기준으로 정렬
                    .ToList();


                return filteredAllTickers;
            }
            else
            {
                Console.WriteLine($"API 요청 실패: {response.StatusCode} - {response.Content}");
                return null;
            }
        }


        #endregion







    }
}

