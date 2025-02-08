using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using static GGo_v2.Upbit_API_v2;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

namespace GGo_v2
{
    public class Upbit_API_v2
    {
        private readonly string _accessKey;
        private readonly string _secretKey;
        public Upbit_API_v2(string AccessKey, string SecretKey)
        {
            _accessKey = AccessKey;
            _secretKey = SecretKey;

            // 명령 처리 Task 실행
            Task.Run(ProcessCommandsAsync);

            // 마켓 리스트 조회
            Request_MarketList();

            // Ticker 조회
            Request_Ticker();

            // 예외처리 코인 등록
            Init_ExceptionCoins();

            // 데이터 업데이터
            _bworker_DataUpdator.DoWork += _bworker_DataUpdator_DoWork;
            _bworker_DataUpdator.RunWorkerAsync();
        }






        #region Data Classes


        // 정보 업데이트. (계좌, Ticker, Candle, Orderbook, OrderList)
        UpbitData _UpbitData = new UpbitData();
        public class UpbitData
        {
            public UpbitData()
            {
                Accounts = new List<Accounts>();
                Tickers = new List<Ticker>();
                CandleData = new CandleData();
                OrderResults = new OrderResults();
                Orderbooks = new List<Orderbook>();
                PendingOrders = new List<PendingOrder>();
            }

            public List<Accounts> Accounts;
            public List<Ticker> Tickers;
            public CandleData CandleData;
            public OrderResults OrderResults;
            public List<Orderbook> Orderbooks;
            public List<PendingOrder> PendingOrders;
        }


        UpbitData_Updated _UpbitData_Updated = new UpbitData_Updated();
        public class UpbitData_Updated
        {
            public UpbitData_Updated()
            {
                IsAccountUpdated = false;
                IsTickerUpdated = false;
                IsCandleUpdated = false;
                IsOrderbookUpdated = false;
                IsOrderlistUpdated = false;
                IsPendingOrderUpdated = false;
            }


            public bool IsUpdatedAll()
            {
                if ((IsCandleUpdated) && (IsAccountUpdated) && (IsOrderlistUpdated) && (IsOrderbookUpdated) && (IsTickerUpdated) && (IsPendingOrderUpdated))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            // 정보 업데이트 여부 (전부 다 업데이트 되면 한번에)
            public bool IsAccountUpdated;
            public bool IsTickerUpdated;
            public bool IsCandleUpdated;
            public bool IsOrderbookUpdated;
            public bool IsOrderlistUpdated;
            public bool IsPendingOrderUpdated;
        }



        // 기본 정보
        public class Info
        {
            public string KRW { get; set; }                     // 보유 원화

            public double ChangeRate_BitCoin { get; set; }     // 비트코인 변화율

            public int N_of_RISE { get; set; }                 // 상승 코인 수
            public int N_of_FALL { get; set; }                  // 하락 코인 수
            public int N_of_EVEN { get; set; }                  // 하락 코인 수


            // 분 캔들 관련
            public string Candle_TargetCoinName { get; set; }
            public int Candle_Minutes { get; set; }
            public int Candle_MA_Period { get; set; }
            public int Candle_N_of_Candles { get; set; }
        }




        // 계좌 정보
        public class Accounts
        {
            public string CoinName { get; set; }        // 코인명 영문
            public string CoinNameKR { get; set; }      // 코인명 한글
            public string Volume { get; set; }          // 보유 볼륨
            public string Volume_Pending { get; set; }  // 미체결 볼륨
            public string AvgBuyPrice { get; set; }     // 매수평균가

            public bool IsExist {  get; set; }          // 보유 상태
            public decimal CurrentPrice { get; set; }   // 현재가
            public decimal Profit { get; set; }         // 수익
            public decimal ProfitRate {  get; set; }    // 수익률
        }

        // 코인 리스트
        public class CoinList
        {
            public string CoinName { get; set; }        // 코인명 영문
            public string CoinNameKR { get; set; }      // 코인명 한글
        }

        // Ticker
        public class Ticker
        {
            public string CoinName { get; set; }        // 코인명 영문
            public string CoinNameKR { get; set; }      // 코인명 한글

            public double Opening_Price {  get; set; }         // 시가
            public double High_Price { get; set; }             // 고가
            public double Low_Price { get; set; }              // 저가
            public double Trade_Price { get; set; }            // 종가
            public double Prev_Trade_Price { get; set; }       // 전일 종가

            public string Change {  get; set; }                // 보합,상승,하락
            public double Change_Price { get; set; }           // 변화액
            public double Change_Rate { get; set; }            // 변화율
            public double Trade_Volume {  get; set; }          // 거래량

            public DateTime Datetime { get; set; }             // 타임스탬프
        }

        // 주문 결과
        public class Order
        {
            public string CoinName { get; set; }        // 코인명 영문
            public string CoinNameKR { get; set; }      // 코인명 한글

            public string Type {  get; set; }           // 매수, 매도
        }


        // 캔들 데이터 통합
        public class CandleData
        {
            public List<Candle> Candles { get; set; }

            public ChartArea ChartArea { get; set; }

            public Series candleSeries { get; set; }

            // 볼린저
            public List<Series> bollingerSeries { get; set; }


            // 봉우리, 골
            public List<Candle> Peaks { get; set; }
            public List<Candle> Troughs { get; set; }
            public Series peakSeries { get; set; }
            public Series troughSeries { get; set; }
        }



        // 분봉
        public class Candle
        {
            public string CoinName { get; set; }        // 코인명 영문
            public string CoinNameKR { get; set; }      // 코인명 한글

            public int Minutes { get; set; }            // 몇분봉

            public DateTime Datetime { get; set; }      // 타임스탬프

            public double Opening_Price { get; set; }   // 시가
            public double High_Price { get; set; }      // 고가
            public double Low_Price { get; set; }       // 저가
            public double Trade_Price { get; set; }     // 종가

            public int MA_Period {  get; set; }             // 이동평균선 기준
            public double MA {  get; set; }                 // 이동평균선
            public double Bollinger_Upper { get; set; }     // 볼린저 상단
            public double Bollinger_Middle { get; set; }    // 볼린저 중단
            public double Bollinger_Lower { get; set; }     // 볼린저 하단

            public string Red_or_Blue { get; set; }         // 양봉 음봉 여부

            // 봉우리, 골
            public double Peak {  get; set; }
            public double Trough {  get; set; }
        }


        // 주문 리스트 및 수익금 (주문 결과)
        public class OrderResults
        {
            public List<OrderResult_by_Trade> orderResults_Trade {  get; set; }    // 개별 거래에 대한 거래 결과
            public List<OrderResult_by_Day> ordereResults_Day { get; set; }      // 일별 수익 결과

            public double TotalProfit { get; set; }                 // 총 수익금
            public double TotalProfitRate { get; set; }             // 총 수익률
        }


        public class OrderResult_by_Trade
        {
            public string CoinName { get; set; }        // 코인명 영문
            public string CoinNameKR { get; set; }      // 코인명 한글

            public double BuyPrice { get; set; }        // 매수 정산금
            public double SellPrice { get; set; }        // 매도 정산금

            public double Profit { get; set; }         // 수익금
            public double ProfitRate { get; set; }      // 수익률

            public DateTime dateTime { get; set; }      // 시간
        }

        public class OrderResult_by_Day
        {
            public double BuyPrice { get; set; }        // 매수 정산금
            public double SellPrice { get; set; }        // 매도 정산금

            public double Profit { get; set; }         // 수익금
            public double ProfitRate { get; set; }      // 수익률

            public DateTime dateTime { get; set; }      // 시간
        }


        // 호가
        public class Orderbook
        {
            public double Price { get; set; }
            public double Volume { get; set; }
            public double Percent {  get; set; }
        }


        // 미체결
        public class PendingOrder
        {
            [JsonProperty("uuid")]
            public string Uuid { get; set; } // 주문의 고유 아이디

            [JsonProperty("side")]
            public string Side { get; set; } // 주문 종류

            [JsonProperty("ord_type")]
            public string OrderType { get; set; } // 주문 방식

            [JsonProperty("price")]
            public string Price { get; set; } // 주문 당시 화폐 가격 (NumberString)

            [JsonProperty("state")]
            public string State { get; set; } // 주문 상태

            [JsonProperty("market")]
            public string Market { get; set; } // 마켓의 유일키

            [JsonProperty("volume")]
            public string Volume { get; set; } // 사용자가 입력한 주문 양 (NumberString)

            [JsonProperty("locked")]
            public string Locked { get; set; } // 거래에 사용 중인 비용 (NumberString)
        }

        #endregion



        #region Public Methods


        // 캔들 정보 업데이트
        public void Set_TargetCandleInfo(string CoinName, int Minute, int MA_Period, int N_of_Candles)
        {
            _Info.Candle_TargetCoinName = CoinName;
            _Info.Candle_Minutes = Minute;
            _Info.Candle_MA_Period = MA_Period;
            _Info.Candle_N_of_Candles = N_of_Candles;
        }


        // 시장가 매수
        public void Request_Buy_Market(string CoinName, string Price)
        {
            UpbitCommandParameters para = new UpbitCommandParameters();
            para.CoinName = CoinName;
            para.Order_Price = Price;

            AddCommand(UpbitCommands.시장가매수, para);
        }

        // 시장가 매도
        public void Request_Sell_Market(string CoinName)
        {
            // 계좌에 있는지 확인
            var account = _Recent_Accounts.FirstOrDefault(a => a.CoinName == CoinName);
            if (account != null)
            {
                if (account.Volume == "0")
                {
                    // 미체결 상태임
                    OrderRetrieved?.Invoke(this, new OrderEventArgs(null, "시장가 매도 실패. 미체결 상태임."));
                }
                else
                {
                    UpbitCommandParameters para = new UpbitCommandParameters();
                    para.CoinName = CoinName;
                    para.Order_Volume = account.Volume;

                    AddCommand(UpbitCommands.시장가매도, para);
                }
            }
            else
            {
                // 계좌에 없음
                OrderRetrieved?.Invoke(this, new OrderEventArgs(null, "시장가 매도 실패. 계좌에 없음."));
            }
        }



        // 지정가 매도 주문
        public void Request_Sell_Limit(string CoinName, string LimitPrice)
        {
            // 계좌에 있는지 확인
            var account = _Recent_Accounts.FirstOrDefault(a => a.CoinName == CoinName);
            if (account != null)
            {
                if (account.Volume == "0")
                {
                    // 미체결 상태임
                    OrderRetrieved?.Invoke(this, new OrderEventArgs(null, "지정가 매도 실패. 미체결 상태임."));
                }
                else
                {
                    UpbitCommandParameters para = new UpbitCommandParameters();
                    para.CoinName = CoinName;
                    para.Limit_Price = LimitPrice;
                    para.Order_Volume = account.Volume;

                    AddCommand(UpbitCommands.지정가매도, para);
                }
            }
            else
            {
                // 계좌에 없음
                OrderRetrieved?.Invoke(this, new OrderEventArgs(null, "지정가 매도 실패. 계좌에 없음."));
            }
        }

        // 지정가 매수 주문
        public void Request_Buy_Limit(string CoinName, string LimitPrice, string BuyingPrice)
        {
            UpbitCommandParameters para = new UpbitCommandParameters();
            para.CoinName = CoinName;
            para.Limit_Price = LimitPrice;
            para.Order_Volume = (decimal.Parse(BuyingPrice) / decimal.Parse(LimitPrice)).ToString();

            AddCommand(UpbitCommands.지정가매수, para);
        }


        // 미체결 취소
        public void Request_Cancel_PendingOrder(string CoinName)
        {

            UpbitCommandParameters para = new UpbitCommandParameters();
            para.CoinName = CoinName;

            AddCommand(UpbitCommands.미체결취소, para);
        }


        #endregion



        #region Recent Data

        private Info _Info = new Info();

        public List<CoinList> CoinListData = new List<CoinList>();

        private List<Accounts> _Recent_Accounts = new List<Accounts>();

        private List<Ticker> _Recent_Tickers = new List<Ticker>();


        #endregion




        #region Private Methods


        // 계좌 조회
        private void Request_Accounts()
        {
            AddCommand(UpbitCommands.계좌조회);
        }

        // 마켓 리스트 조회 (거래 가능 코인명 리스트)
        private void Request_MarketList()
        {
            AddCommand(UpbitCommands.마켓리스트조회);
        }

        // Ticker 조회
        private void Request_Ticker()
        {
            AddCommand(UpbitCommands.Ticker조회);
        }


        // 분 캔들 조회
        private void Request_Candle()
        {
            UpbitCommandParameters para = new UpbitCommandParameters();
            para.CoinName = _Info.Candle_TargetCoinName;
            para.Candle_Minute = _Info.Candle_Minutes;
            para.Candle_MA_Period = _Info.Candle_MA_Period;
            para.Candle_N_of_Candles = _Info.Candle_N_of_Candles;

            AddCommand(UpbitCommands.분캔들조회, para);
        }



        // 주문 리스트 (완료된 거래) 조회
        private void Request_OrderList()
        {
            AddCommand(UpbitCommands.주문리스트조회);
        }



        // 미체결 조회
        private void Request_PendingOrders()
        {
            AddCommand(UpbitCommands.미체결조회);
        }


        // 호가 조회
        private void Request_Orderbook()
        {
            AddCommand(UpbitCommands.호가조회);
        }



        #endregion



        #region Data Update Background Worker


        // 정해진 시간마다 데이터 업데이트 (ticker, account 등)
        BackgroundWorker _bworker_DataUpdator = new BackgroundWorker();


        private void _bworker_DataUpdator_DoWork(object sender, DoWorkEventArgs e)
        {
            // 기본 정보 업데이트 확인 대기
            while(_Recent_Tickers.Count == 0)
            {
                Thread.Sleep(200);
            }


            Stopwatch stopwatch = new Stopwatch();

            while (true)
            {
                stopwatch.Restart();  // 시작 시간 기록

                Request_Accounts();
                Request_Ticker();
                Request_OrderList();
                Request_PendingOrders();

                if (_Info.Candle_TargetCoinName != null)
                {
                    Request_Candle();
                    Request_Orderbook();
                }

                

                // 최대 1초 동안 IsUpdatedAll()이 true가 될 때까지 기다림
                Stopwatch waitTimer = new Stopwatch();
                waitTimer.Start();

                while (!_UpbitData_Updated.IsUpdatedAll() && waitTimer.ElapsedMilliseconds < 1000)
                {
                    Thread.Sleep(10); // CPU 점유율을 줄이기 위해 잠시 대기
                }

                waitTimer.Stop();

                if (_UpbitData_Updated.IsUpdatedAll())
                {
                    UpbitDataRetrieved?.Invoke(this, new UpbitDataEventArgs(_UpbitData));
                }
                else
                {
                    // 1초 내에 업데이트되지 않으면 데이터 초기화
                    _UpbitData = new UpbitData();
                    _UpbitData_Updated = new UpbitData_Updated();
                }

                stopwatch.Stop();

                // 실행 시간 고려하여 정확히 1초 주기 유지
                int elapsed = (int)stopwatch.ElapsedMilliseconds;
                int remainingTime = 1000 - elapsed;
                if (remainingTime > 0)
                {
                    Thread.Sleep(remainingTime);
                }
            }



        }


        #endregion




        #region API Methods

        // Accounts Authorization Token 생성
        private string Get_AuthorizationToken(Dictionary<string, string> queryParams = null)
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


        // 계좌 조회
        private async Task Get_Accounts_API()
        {
            string authorizationToken = Get_AuthorizationToken();

            var client = new RestClient("https://api.upbit.com/v1/accounts");
            var request = new RestRequest();
            request.Method = Method.Get;
            request.AddHeader("Authorization", authorizationToken);

            var response = await client.ExecuteAsync(request);

            List<Upbit_Account> accounts = null;
            if (response.IsSuccessful)
            {
                accounts = JsonConvert.DeserializeObject<List<Upbit_Account>>(response.Content);
            }
            else
            {
                Console.WriteLine($"API 요청 실패: {response.StatusCode} - {response.Content}");
            }

            // 계좌 있는지 확인
            if (accounts != null && accounts.Count > 0)
            {
                List<Accounts> tempAccounts = new List<Accounts>();

                // 기본 내용 옮기기
                tempAccounts = accounts
                    .Select(s => new Accounts {
                        CoinName = s.Currency,
                        CoinNameKR = s.Currency != "KRW" ? CoinListData.FirstOrDefault(c => c.CoinName == s.Currency).CoinNameKR : "",
                        Volume = s.Balance, 
                        Volume_Pending = s.Locked, 
                        AvgBuyPrice = s.AvgBuyPrice })
                    .ToList();

                // KRW는 제거
                tempAccounts.RemoveAll(a => a.CoinName == "KRW");

                // 나머지 내용 체우기
                _Info.KRW = accounts.FirstOrDefault(b => b.Currency == "KRW").Balance;


                // 최근 정보에 저장
                _Recent_Accounts = tempAccounts.ToList();


                // Deep Copy
                List<Accounts> ret = tempAccounts.Select(a => new Accounts 
                {
                    CoinName = a.CoinName,
                    CoinNameKR = a.CoinNameKR,
                    Volume = a.Volume,
                    Volume_Pending = a.Volume_Pending,
                    AvgBuyPrice = a.AvgBuyPrice,
                    IsExist = a.IsExist,
                    CurrentPrice = a.CurrentPrice,
                    Profit = a.Profit,
                    ProfitRate = a.ProfitRate
                }
                ).ToList();


                // 최근 값 저장 및 플래그 셋
                _UpbitData_Updated.IsAccountUpdated = true;
                _UpbitData.Accounts = ret.ToList();
            }
            else
            {
                Console.WriteLine($"API 요청 실패: {response.StatusCode} - {response.Content}");
            }
        }


        // 코인(마켓)리스트 가져오기
        private async Task Get_CoinList_API()
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
                // JSON 데이터를 List<Upbit_Market> 형식으로 역직렬화
                List<Upbit_Market> allCoins = JsonConvert.DeserializeObject<List<Upbit_Market>>(response.Content);

                // 코인 리스트에 저장
                CoinListData = allCoins
                    .Where(coin => coin.Name_Market.StartsWith("KRW-")) // KRW-로 시작하는 항목만 필터링
                    .Select(coin => new CoinList
                    {
                        CoinName = coin.Name_Market.Replace("KRW-", ""), // KRW- 제거
                        CoinNameKR = coin.Name_KR,
                    })
                    .OrderBy(coin => coin.CoinNameKR) // Name_KR 기준으로 정렬
                    .ToList();
            }
            else
            {
                Console.WriteLine($"API 요청 실패: {response.StatusCode} - {response.Content}");
            }
        }


        // Ticker 조회
        private async Task Get_Ticker_API()
        {
            string authorizationToken = Get_AuthorizationToken();

            var client = new RestClient("https://api.upbit.com/v1/ticker/all?quote_currencies=KRW");
            var request = new RestRequest();
            request.Method = Method.Get;
            request.AddHeader("Authorization", authorizationToken);

            var response = await client.ExecuteAsync(request);

            //Console.WriteLine(response.Content);

            if (response.IsSuccessful)
            {
                // JSON 데이터를 List<TickerData> 형식으로 역직렬화
                var allTickers = JsonConvert.DeserializeObject<List<Upbit_Ticker>>(response.Content);

                // KRW-로 시작하는 코인만 필터링
                List<Ticker> filteredAllTickers = allTickers
                    .Where(ticker => ticker.Market.StartsWith("KRW-")) // KRW-로 시작하는 항목만 필터링
                    .Select(ticker => new Ticker
                    {
                        CoinName = ticker.Market.Replace("KRW-", ""), // KRW- 제거
                        CoinNameKR = CoinListData.FirstOrDefault(c => c.CoinName == ticker.Market.Replace("KRW-", "")).CoinNameKR,

                        Opening_Price = ticker.OpeningPrice,
                        High_Price = ticker.HighPrice,
                        Low_Price = ticker.LowPrice,
                        Trade_Price = ticker.TradePrice,
                        Prev_Trade_Price = ticker.PrevClosingPrice,

                        Change = ticker.Change == "FALL" ? "하락" : ticker.Change == "RISE" ? "상승" : "보합",
                        Change_Price = ticker.SignedChangePrice,
                        Change_Rate = ticker.SignedChangeRate,
                        Trade_Volume = ticker.TradeVolume,
                        Datetime = DateTimeOffset.FromUnixTimeMilliseconds(ticker.Timestamp).UtcDateTime.AddHours(9)
                    })
                    .OrderBy(ticker => ticker.CoinNameKR) // Market 기준으로 정렬
                    .ToList();

                // Info 불러와서 업데이트
                _Info.N_of_RISE = filteredAllTickers.FindAll(rise => rise.Change == "상승").Count;
                _Info.N_of_EVEN = filteredAllTickers.FindAll(rise => rise.Change == "보합").Count;
                _Info.N_of_FALL = filteredAllTickers.FindAll(rise => rise.Change == "하락").Count;

                _Info.ChangeRate_BitCoin = filteredAllTickers.FirstOrDefault(b => b.CoinNameKR == "비트코인").Change_Rate;

                // 최근 ticker 업데이트
                _Recent_Tickers = filteredAllTickers.ToList();

                // Deep Copy
                List<Ticker> ret = filteredAllTickers.Select(a => new Ticker
                {
                    CoinName = a.CoinName,
                    CoinNameKR = a.CoinNameKR,
                    Opening_Price = a.Opening_Price,
                    High_Price = a.High_Price,
                    Low_Price = a.Low_Price,
                    Trade_Price = a.Trade_Price,
                    Prev_Trade_Price = a.Prev_Trade_Price,
                    Change = a.Change,
                    Change_Rate = a.Change_Rate,
                    Change_Price = a.Change_Price,
                    Trade_Volume = a.Trade_Volume,
                    Datetime = a.Datetime
                }
                ).ToList();


                // 최근 값 저장 및 플래그 셋
                _UpbitData_Updated.IsTickerUpdated = true;
                _UpbitData.Tickers = ret.ToList();

            }
            else
            {
                Console.WriteLine($"API 요청 실패: {response.StatusCode} - {response.Content}");
            }

        }

        // 분 캔들 조회
        private async Task Get_Candle_Minutes_API(string CoinName, int Minute, int MA_Period, int N_of_Candles)
        {
            // Query Parameter
            var queryParams = new Dictionary<string, string>
            {
                { "market", $"KRW-{CoinName}" },
                { "count", "200" }
            };

            // Authorization Token 생성
            string authorizationToken = Get_AuthorizationToken(queryParams);

            // RestClient 및 Request 구성
            var client = new RestClient($"https://api.upbit.com/v1/candles/minutes/{Minute}");
            var request = new RestRequest();
            request.Method = Method.Get;

            // Query Parameters 추가
            foreach (var param in queryParams)
            {
                request.AddQueryParameter(param.Key, param.Value);
            }

            // Authorization 헤더 추가
            request.AddHeader("Authorization", authorizationToken);

            // 요청 실행
            var response = await client.ExecuteAsync(request);

            // 응답 처리
            if (response.IsSuccessful)
            {

                // 캔들 가져오기
                List<Upbit_Candle_Minutes> tempCandles = JsonConvert.DeserializeObject<List<Upbit_Candle_Minutes>>(response.Content);

                tempCandles = tempCandles.OrderBy(c => c.candle_date_time_kst).ToList(); // 날짜순 정렬 후 리스트 업데이트

                string coinName = _Info.Candle_TargetCoinName;
                string coinNameKR = CoinListData.FirstOrDefault(a => a.CoinName == _Info.Candle_TargetCoinName).CoinNameKR;

                // Deep Copy
                List<Candle> candle_ret = tempCandles.Select(a => new Candle
                {
                    CoinName = coinName,
                    CoinNameKR = coinNameKR,

                    Minutes = a.unit,
                    Datetime = DateTime.ParseExact(a.candle_date_time_kst, "yyyy-MM-dd'T'HH:mm:ss", null),

                    Opening_Price = a.opening_price,
                    High_Price = a.high_price,
                    Low_Price = a.low_price,
                    Trade_Price = a.trade_price,

                    MA_Period = MA_Period
                }
                ).ToList();



                for (int i = 0; i < candle_ret.Count; i++)
                {
                    if (i + 1 >= MA_Period)
                    {
                        // 이동평균 계산
                        var subset = candle_ret.Skip(i + 1 - MA_Period).Take(MA_Period);
                        double movingAverage = subset.Average(c => c.Trade_Price);

                        // 표준편차 계산
                        double standardDeviation = Math.Sqrt(subset.Average(c => Math.Pow(c.Trade_Price - movingAverage, 2)));

                        // 볼린저 밴드 계산
                        candle_ret[i].MA = movingAverage;
                        candle_ret[i].Bollinger_Upper = movingAverage + (2 * standardDeviation);
                        candle_ret[i].Bollinger_Middle = movingAverage;
                        candle_ret[i].Bollinger_Lower = movingAverage - (2 * standardDeviation);

                        // 양봉, 음봉 계산
                        candle_ret[i].Red_or_Blue =
                            candle_ret[i].Trade_Price > candle_ret[i].Opening_Price ? "Red" :
                            candle_ret[i].Trade_Price < candle_ret[i].Opening_Price ? "Blue" : "Doge";
                    }
                }

                // 볼린저 없는 만큼 잘라냄
                candle_ret = candle_ret.Skip(MA_Period - 1).ToList();

                // 요청 데이터 개수만큼만 최근기준으로 남기기
                candle_ret = candle_ret.Skip(candle_ret.Count - N_of_Candles).Take(N_of_Candles).ToList();

                
                // Chart Area
                ChartArea chartArea = new ChartArea("MinuteCandle");
                chartArea.AxisX.MajorGrid.LineColor = Color.FromArgb(25, Color.Black); // 10% 투명도 (255 * 0.1 = 25)
                chartArea.AxisY.MajorGrid.LineColor = Color.FromArgb(25, Color.Black); // 10% 투명도
                double MinY = candle_ret.Min(candle => candle.Low_Price) * 0.99; // low_price 중 최소값
                double MaxY = candle_ret.Max(candle => candle.High_Price) * 1.01; // high_price 중 최대값
                chartArea.AxisY.Minimum = MinY;
                chartArea.AxisY.Maximum = MaxY;
                chartArea.AxisX.LabelStyle.Format = "HH:mm"; // X축 시간 포맷
                chartArea.AxisY.IsStartedFromZero = false;  // Y축 0부터 시작 안 함


                // 캔들 시리즈 만들기
                Series candleSeries = Create_CandleSeries_from_List(candle_ret);

                // 볼린저 series
                List<Series> bollingerSeries = Create_BollingerSeries_from_List(candle_ret).ToList();

                // 봉우리, 골
                List<Candle> peaks = new List<Candle>();
                for (int i = 1; i < candle_ret.Count - 1; i++)
                {
                    if (candle_ret[i].High_Price > candle_ret[i - 1].High_Price && candle_ret[i].High_Price > candle_ret[i + 1].High_Price)
                    {
                        candle_ret[i].Peak = candle_ret[i].High_Price;
                        peaks.Add(candle_ret[i]);
                    }
                }
                List<Candle> troughs = new List<Candle>();
                for (int i = 1; i < candle_ret.Count - 1; i++)
                {
                    if (candle_ret[i].Low_Price < candle_ret[i - 1].Low_Price && candle_ret[i].Low_Price < candle_ret[i + 1].Low_Price)
                    {
                        candle_ret[i].Trough = candle_ret[i].Low_Price;
                        troughs.Add(candle_ret[i]);
                    }
                }



                // 봉우리, 골 series
                Series peakSeries = Create_PeakNtroughSeries("봉우리");
                Series troughSeries = Create_PeakNtroughSeries("골");

                // 최근 값 저장 및 플래그 셋
                _UpbitData_Updated.IsCandleUpdated = true;
                _UpbitData.CandleData.Candles = candle_ret.ToList();
                _UpbitData.CandleData.candleSeries = candleSeries;
                _UpbitData.CandleData.bollingerSeries = bollingerSeries.ToList();
                _UpbitData.CandleData.ChartArea = chartArea;
                _UpbitData.CandleData.Peaks = peaks.ToList();
                _UpbitData.CandleData.Troughs = troughs.ToList();
                _UpbitData.CandleData.peakSeries = peakSeries;
                _UpbitData.CandleData.troughSeries = troughSeries;

            }
            else
            {
                Console.WriteLine($"API 요청 실패: {response.StatusCode} - {response.Content}");
            }
        }

        // 시장가 매수 요청
        private async Task Order_Buy_Market_API(string CoinName, string Price)
        {
            // 1. Query 문자열 생성
            var query = $"market=KRW-{CoinName}&side=bid&volume=&price={Price}&ord_type=price";

            // 2. Query Hash 생성 (SHA512)
            string queryHash;
            using (SHA512 sha512 = SHA512.Create())
            {
                byte[] hash = sha512.ComputeHash(Encoding.UTF8.GetBytes(query));
                queryHash = BitConverter.ToString(hash).Replace("-", "").ToLower();
            }

            // 3. JWT 토큰 생성
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim("access_key", _accessKey),
                new System.Security.Claims.Claim("nonce", Guid.NewGuid().ToString()),
                new System.Security.Claims.Claim("query_hash", queryHash),
                new System.Security.Claims.Claim("query_hash_alg", "SHA512")
            };

            var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var header = new JwtHeader(credentials);
            var payload = new JwtPayload(claims);
            var secToken = new JwtSecurityToken(header, payload);
            var jwtToken = new JwtSecurityTokenHandler().WriteToken(secToken);

            // 4. HTTP 요청 생성
            var client = new RestClient("https://api.upbit.com/v1/orders");
            var request = new RestRequest();
            request.Method = Method.Post;
            request.AddHeader("Authorization", $"Bearer {jwtToken}");
            request.AddHeader("Content-Type", "application/json");

            // 5. 요청 본문 생성 및 추가
            var body = new
            {
                market = "KRW-" + CoinName,
                side = "bid",
                volume = "",
                price = Price,
                ord_type = "price"
            };
            request.AddJsonBody(body);

            // 6. API 요청 및 응답 처리
            var response = await client.ExecuteAsync(request);
            if (response.IsSuccessful)
            {
                Console.WriteLine("주문 요청 성공!");
                Order order = new Order();

                order.CoinName = CoinName;
                order.CoinNameKR = CoinListData.FirstOrDefault(c => c.CoinName == CoinName).CoinNameKR;
                order.Type = "시장가매수";

                // 성공 시 이벤트 발생
                OrderRetrieved?.Invoke(this, new OrderEventArgs(order, "주문 성공"));
            }
            else
            {
                Console.WriteLine($"주문 요청 실패: {response.StatusCode} - {response.Content}");
                // 실패 시 이벤트 발생
                OrderRetrieved?.Invoke(this, new OrderEventArgs(null, "주문 실패"));
            }
        }

        // 시장가 매도 요청
        private async Task Order_Sell_Market_API(string CoinName, string Volume)
        {
            // 1. Query 문자열 생성
            var query = $"market=KRW-{CoinName}&side=ask&volume={Volume}&price=&ord_type=market";

            // 2. Query Hash 생성 (SHA512)
            string queryHash;
            using (SHA512 sha512 = SHA512.Create())
            {
                byte[] hash = sha512.ComputeHash(Encoding.UTF8.GetBytes(query));
                queryHash = BitConverter.ToString(hash).Replace("-", "").ToLower();
            }

            // 3. JWT 토큰 생성
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim("access_key", _accessKey),
                new System.Security.Claims.Claim("nonce", Guid.NewGuid().ToString()),
                new System.Security.Claims.Claim("query_hash", queryHash),
                new System.Security.Claims.Claim("query_hash_alg", "SHA512")
            };

            var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var header = new JwtHeader(credentials);
            var payload = new JwtPayload(claims);
            var secToken = new JwtSecurityToken(header, payload);
            var jwtToken = new JwtSecurityTokenHandler().WriteToken(secToken);

            // 4. HTTP 요청 생성
            var client = new RestClient("https://api.upbit.com/v1/orders");
            var request = new RestRequest();
            request.Method = Method.Post;
            request.AddHeader("Authorization", $"Bearer {jwtToken}");
            request.AddHeader("Content-Type", "application/json");

            // 5. 요청 본문 생성 및 추가
            var body = new
            {
                market = "KRW-" + CoinName,
                side = "ask",
                volume = Volume,
                price = "",
                ord_type = "market"
            };
            request.AddJsonBody(body);

            // 6. API 요청 및 응답 처리
            var response = await client.ExecuteAsync(request);
            if (response.IsSuccessful)
            {
                Console.WriteLine("주문 요청 성공!");
                Order order = new Order();

                order.CoinName = CoinName;
                order.CoinNameKR = CoinListData.FirstOrDefault(c => c.CoinName == CoinName).CoinNameKR;
                order.Type = "시장가매도";

                // 성공 시 이벤트 발생
                OrderRetrieved?.Invoke(this, new OrderEventArgs(order, "주문 성공"));
            }
            else
            {
                Console.WriteLine($"주문 요청 실패: {response.StatusCode} - {response.Content}");
                // 실패 시 이벤트 발생
                OrderRetrieved?.Invoke(this, new OrderEventArgs(null, "주문 실패"));
            }
        }


        // 지정가 매도 요청
        private async Task Order_Sell_Limit_API(string CoinName, string LimitPrice, string Volume)
        {
            // 소수점 맞추기
            string adjustedPrice = GetAdjustedLimitPrice(CoinName, decimal.Parse(LimitPrice)).ToString();

            // API 키 설정
            string accessKey = _accessKey;
            string secretKey = _secretKey;

            // 1. Query 문자열 생성
            var query = $"market=KRW-{CoinName}&side=ask&volume={Volume}&price={adjustedPrice}&ord_type=limit";

            // 2. Query Hash 생성 (SHA512)
            string queryHash;
            using (SHA512 sha512 = SHA512.Create())
            {
                byte[] hash = sha512.ComputeHash(Encoding.UTF8.GetBytes(query));
                queryHash = BitConverter.ToString(hash).Replace("-", "").ToLower();
            }

            // 3. JWT 토큰 생성
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim("access_key", accessKey),
                new System.Security.Claims.Claim("nonce", Guid.NewGuid().ToString()),
                new System.Security.Claims.Claim("query_hash", queryHash),
                new System.Security.Claims.Claim("query_hash_alg", "SHA512")
            };

            var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var header = new JwtHeader(credentials);
            var payload = new JwtPayload(claims);
            var secToken = new JwtSecurityToken(header, payload);
            var jwtToken = new JwtSecurityTokenHandler().WriteToken(secToken);

            // 4. HTTP 요청 생성
            var client = new RestClient("https://api.upbit.com/v1/orders");
            var request = new RestRequest();
            request.Method = Method.Post;
            request.AddHeader("Authorization", $"Bearer {jwtToken}");
            request.AddHeader("Content-Type", "application/json");

            // 5. 요청 본문 생성 및 추가
            var body = new
            {
                market = "KRW-" + CoinName,
                side = "ask",
                volume = Volume,
                price = adjustedPrice,
                ord_type = "limit"
            };
            request.AddJsonBody(body);


            // 6. API 요청 및 응답 처리
            var response = await client.ExecuteAsync(request);
            if (response.IsSuccessful)
            {
                Console.WriteLine("주문 요청 성공!");
                Order order = new Order();

                order.CoinName = CoinName;
                order.CoinNameKR = CoinListData.FirstOrDefault(c => c.CoinName == CoinName).CoinNameKR;
                order.Type = "지정가매도";

                // 성공 시 이벤트 발생
                OrderRetrieved?.Invoke(this, new OrderEventArgs(order, "주문 성공"));
            }
            else
            {
                Console.WriteLine($"주문 요청 실패: {response.StatusCode} - {response.Content}");
                // 실패 시 이벤트 발생
                OrderRetrieved?.Invoke(this, new OrderEventArgs(null, "주문 실패"));
            }
        }



        // 지정가 매수 요청
        private async Task Order_Buy_Limit_API(string CoinName, string LimitPrice, string Volume)
        {
            // 소수점 맞추기
            string adjustedPrice = GetAdjustedLimitPrice(CoinName, decimal.Parse(LimitPrice)).ToString();


            // API 키 설정
            string accessKey = _accessKey;
            string secretKey = _secretKey;

            // 1. Query 문자열 생성
            var query = $"market=KRW-{CoinName}&side=bid&volume={Volume}&price={adjustedPrice}&ord_type=limit";

            // 2. Query Hash 생성 (SHA512)
            string queryHash;
            using (SHA512 sha512 = SHA512.Create())
            {
                byte[] hash = sha512.ComputeHash(Encoding.UTF8.GetBytes(query));
                queryHash = BitConverter.ToString(hash).Replace("-", "").ToLower();
            }

            // 3. JWT 토큰 생성
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim("access_key", accessKey),
                new System.Security.Claims.Claim("nonce", Guid.NewGuid().ToString()),
                new System.Security.Claims.Claim("query_hash", queryHash),
                new System.Security.Claims.Claim("query_hash_alg", "SHA512")
            };

            var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var header = new JwtHeader(credentials);
            var payload = new JwtPayload(claims);
            var secToken = new JwtSecurityToken(header, payload);
            var jwtToken = new JwtSecurityTokenHandler().WriteToken(secToken);

            // 4. HTTP 요청 생성
            var client = new RestClient("https://api.upbit.com/v1/orders");
            var request = new RestRequest();
            request.Method = Method.Post;
            request.AddHeader("Authorization", $"Bearer {jwtToken}");
            request.AddHeader("Content-Type", "application/json");

            // 5. 요청 본문 생성 및 추가
            var body = new
            {
                market = "KRW-" + CoinName,
                side = "bid",
                volume = Volume,
                price = adjustedPrice,
                ord_type = "limit"
            };
            request.AddJsonBody(body);

            // 6. API 요청 및 응답 처리
            var response = await client.ExecuteAsync(request);
            if (response.IsSuccessful)
            {
                Console.WriteLine("주문 요청 성공!");
                Order order = new Order();

                order.CoinName = CoinName;
                order.CoinNameKR = CoinListData.FirstOrDefault(c => c.CoinName == CoinName).CoinNameKR;
                order.Type = "지정가매수";

                // 성공 시 이벤트 발생
                OrderRetrieved?.Invoke(this, new OrderEventArgs(order, "주문 성공"));
            }
            else
            {
                Console.WriteLine($"주문 요청 실패: {response.StatusCode} - {response.Content}");
                // 실패 시 이벤트 발생
                OrderRetrieved?.Invoke(this, new OrderEventArgs(null, "주문 실패"));
            }
        }



        // 주문 리스트 조회
        private async Task Get_OrderList_API()
        {
            // 쿼리 스트링 생성
            var queryParams = new Dictionary<string, string>
        {
            //{ "state", "done" },
            //{ "to", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") },
            { "limit", "1000" }
        };

            var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}"));

            // JWT Header
            var header = new
            {
                alg = "HS256",
                typ = "JWT"
            };

            // JWT Payload
            var payload = new
            {
                access_key = _accessKey,
                nonce = Guid.NewGuid().ToString(), // 유니크한 요청 ID
                query = queryString
            };

            // Base64 URL Encoding
            string EncodeBase64Url(string input)
            {
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(input))
                    .Replace("+", "-")
                    .Replace("/", "_")
                    .Replace("=", "");
            }

            // Header, Payload 직렬화 후 Base64 URL Encoding
            var encodedHeader = EncodeBase64Url(JsonConvert.SerializeObject(header));
            var encodedPayload = EncodeBase64Url(JsonConvert.SerializeObject(payload));

            // Signature 생성 (HMAC-SHA256)
            var secretBytes = Encoding.UTF8.GetBytes(_secretKey);
            var signature = string.Empty;
            using (var hmac = new HMACSHA256(secretBytes))
            {
                var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{encodedHeader}.{encodedPayload}"));
                signature = Convert.ToBase64String(signatureBytes)
                    .Replace("+", "-")
                    .Replace("/", "_")
                    .Replace("=", "");
            }

            // 최종 JWT
            var jwtToken = $"{encodedHeader}.{encodedPayload}.{signature}";

            // API 요청
            var client = new RestClient("https://api.upbit.com/v1/orders/closed");
            var request = new RestRequest();
            request.Method = Method.Get;

            // 쿼리 파라미터 추가
            foreach (var param in queryParams)
            {
                request.AddQueryParameter(param.Key, param.Value);
            }

            // Authorization 헤더 추가
            request.AddHeader("Authorization", $"Bearer {jwtToken}");

            // API 호출
            var response = await client.ExecuteAsync(request);

            if (response.IsSuccessful)
            {
                // JSON 데이터를 UpbitOrder 리스트로 변환
                List<Upbit_OrderList> orderLists = JsonConvert.DeserializeObject<List<Upbit_OrderList>>(response.Content);

                // DateTime 계산
                orderLists.ForEach(dt => dt.CreatedAt_DT = DateTime.Parse(dt.CreatedAt));


                // 수익률 계산
                OrderResults ret = Create_OrderListProfits_from_OrderList(orderLists);


                // 최근 값 저장 및 플래그 셋
                _UpbitData_Updated.IsOrderlistUpdated = true;
                _UpbitData.OrderResults = ret;

            }
            else
            {
                Console.WriteLine($"API 요청 실패: {response.StatusCode} - {response.Content}");
            }
        }


        // 호가 조회
        public async Task Get_Orderbook_API()
        {
            string apiUrl = $"https://api.upbit.com/v1/orderbook?markets={"KRW-" + _Info.Candle_TargetCoinName}";

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    // JSON 응답을 명확한 모델로 역직렬화
                    List<Upbit_Orderbook> orderBooks = JsonConvert.DeserializeObject<List<Upbit_Orderbook>>(content);


                    List<Orderbook> tempOrderbooks = new List<Orderbook>();

                    foreach(var price in orderBooks[0].OrderbookUnits)
                    {
                        Orderbook tempAsk = new Orderbook();
                        tempAsk.Price = price.AskPrice;
                        tempAsk.Volume = price.AskSize;

                        Orderbook tempBid = new Orderbook();
                        tempBid.Price = price.BidPrice;
                        tempBid.Volume = price.BidSize;

                        tempOrderbooks.Add(tempAsk);
                        tempOrderbooks.Add(tempBid);
                    }

                    // 가격으로 재정렬
                    tempOrderbooks.OrderByDescending(x => x.Price).ToList();


                    // 퍼센트 입력
                    double totalVolume = orderBooks[0].TotalBidSize + orderBooks[0].TotalAskSize;
                    foreach (var target in tempOrderbooks)
                    {
                        target.Percent = (target.Volume / totalVolume) * 100;
                    }


                    // 최근 값 저장 및 플래그 셋
                    _UpbitData_Updated.IsOrderbookUpdated = true;
                    _UpbitData.Orderbooks = tempOrderbooks.ToList();
                }
                else
                {
                    Console.WriteLine($"API 요청 실패: {response.StatusCode}");

                }
            }
        }


        // 미체결 내역 조회
        public async Task Get_PendingOrders_API()
        {
            var payload = new JwtPayload
        {
            { "access_key", _accessKey },
            { "nonce", Guid.NewGuid().ToString() }
        };

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var header = new JwtHeader(credentials);
            var token = new JwtSecurityToken(header, payload);
            string jwtToken = new JwtSecurityTokenHandler().WriteToken(token);

            string authorizationToken = $"Bearer {jwtToken}";

            var client = new RestClient("https://api.upbit.com/v1/orders");
            var request = new RestRequest();
            request.Method = Method.Get;
            request.AddHeader("Authorization", authorizationToken);

            var response = await client.ExecuteAsync(request);

            if (response.IsSuccessful)
            {
                // JSON 데이터를 객체로 변환
                var orders = JsonConvert.DeserializeObject<List<PendingOrder>>(response.Content);

                // Market 속성에서 "KRW-" 제거
                foreach (var order in orders)
                {
                    if (order.Market.StartsWith("KRW-"))
                    {
                        order.Market = order.Market.Replace("KRW-", string.Empty);
                    }
                }


                // 최근 값 저장 및 플래그 셋
                _UpbitData_Updated.IsPendingOrderUpdated = true;
                _UpbitData.PendingOrders = orders.ToList();

            }
            else
            {
                Console.WriteLine($"API 요청 실패: {response.StatusCode} - {response.Content}");
                
            }
        }


        // 미체결 거래 취소
        public async Task Cancel_PendingOrder_API(string CoinName)
        {
            // 미체결 내역에 있는지 확인
            var pendingOrder = _UpbitData.PendingOrders.FirstOrDefault(a => a.Market == CoinName);

            if (pendingOrder == null)
            {
                OrderRetrieved?.Invoke(this, new OrderEventArgs(null, "미체결 취소 실패"));
                return;
            }

            string queryString = $"uuid={pendingOrder.Uuid}";


            // JWT 토큰 생성
            // 1. Query Hash 생성 (SHA-512 해시)
            string queryHash;
            using (var sha512 = SHA512.Create())
            {
                var hashBytes = sha512.ComputeHash(Encoding.UTF8.GetBytes(queryString));
                queryHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }

            // 2. JWT Payload 생성
            var payload = new JwtPayload
    {
        { "access_key", _accessKey }, // API 키
        { "nonce", Guid.NewGuid().ToString() }, // 요청 고유값
        { "query_hash", queryHash }, // Query String의 SHA-512 해시값
        { "query_hash_alg", "SHA512" } // 해시 알고리즘 이름
    };

            // 3. Secret Key 설정
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // 4. JWT Header와 Token 생성
            var header = new JwtHeader(credentials);
            var token = new JwtSecurityToken(header, payload);

            // 5. JWT Token 반환
            var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);


            // HTTP 요청
            var client = new RestClient("https://api.upbit.com/v1/order");
            var request = new RestRequest();
            request.Method = Method.Delete;
            request.AddHeader("Authorization", $"Bearer {jwtToken}");
            request.AddQueryParameter("uuid", pendingOrder.Uuid);

            var response = await client.ExecuteAsync(request);


            if (response.IsSuccessful)
            {
                // JSON 데이터를 객체로 변환
                var orders = JsonConvert.DeserializeObject<PendingOrder>(response.Content);

                Order order = new Order();
                order.CoinName = CoinName;
                order.CoinNameKR = CoinListData.FirstOrDefault(c => c.CoinName == CoinName).CoinNameKR;
                order.Type = "미체결취소";

                // 성공 시 이벤트 발생
                OrderRetrieved?.Invoke(this, new OrderEventArgs(order, "미체결 취소 성공"));
            }
            else
            {
                Console.WriteLine($"API 요청 실패: {response.StatusCode} - {response.Content}");
                // 성공 시 이벤트 발생
                OrderRetrieved?.Invoke(this, new OrderEventArgs(null, "미체결 취소 실패"));
            }

        }


        #endregion





        #region 서버 요청 처리 및 제한 핸들러


        private readonly object _cmdLock = new object();
        private readonly List<Upbit_Command> _CMD_Requests = new List<Upbit_Command>();

        // 명령 포맷
        private class Upbit_Command
        {
            public UpbitCommands Command { get; set; }
            public UpbitCommandParameters Parameters { get; set; }
        }

        // 명령어
        private enum UpbitCommands
        {
            계좌조회 = 0,
            마켓리스트조회 = 1,
            Ticker조회 = 2,
            분캔들조회 = 3,
            시장가매수 = 4,
            시장가매도 = 5,
            주문리스트조회 = 6,
            미체결조회 = 7,
            호가조회 = 8,
            지정가매도 = 9,
            지정가매수 = 10,
            미체결취소 = 11
        }

        // 파라메터
        private class UpbitCommandParameters
        {
            public string CoinName { get; set; }

            public string Order_Price {  get; set; } 
            public string Limit_Price { get; set; }      // for limit order
            public string Order_Volume {  get; set; }

            public int Candle_Minute { get; set; }
            public int Candle_MA_Period {  get; set; }
            public int Candle_N_of_Candles { get; set; }
        }



        // 명령 넣기
        private void AddCommand(UpbitCommands command, UpbitCommandParameters parameters = null)
        {
            lock (_cmdLock)
            {
                _CMD_Requests.Add(new Upbit_Command
                {
                    Command = command,
                    Parameters = parameters
                });
            }
        }


        // 서버 요청 처리 태스크
        private async Task ProcessCommandsAsync()
        {
            while (true)
            {
                var command = GetNextCommand();
                if (command != null)
                {
                    switch (command.Command)
                    {
                        case UpbitCommands.계좌조회:
                            await _RequestCounter_Order.ProcessRequestAsync(async () =>
                            {
                                await Get_Accounts_API();
                            });
                            break;
                        case UpbitCommands.마켓리스트조회:
                            await _RequestCounter_Order.ProcessRequestAsync(async () =>
                            {
                                await Get_CoinList_API();
                            });
                            break;
                        case UpbitCommands.Ticker조회:
                            await _RequestCounter_Order.ProcessRequestAsync(async () =>
                            {
                                await Get_Ticker_API();
                            });
                            break;
                        case UpbitCommands.시장가매수:
                            await _RequestCounter_Order.ProcessRequestAsync(async () =>
                            {
                                await Order_Buy_Market_API(command.Parameters.CoinName, command.Parameters.Order_Price);
                            });
                            break;
                        case UpbitCommands.시장가매도:
                            await _RequestCounter_Order.ProcessRequestAsync(async () =>
                            {
                                await Order_Sell_Market_API(command.Parameters.CoinName, command.Parameters.Order_Volume);
                            });
                            break;
                        case UpbitCommands.분캔들조회:
                            await _RequestCounter_Order.ProcessRequestAsync(async () =>
                            {
                                await Get_Candle_Minutes_API(command.Parameters.CoinName, command.Parameters.Candle_Minute, command.Parameters.Candle_MA_Period, command.Parameters.Candle_N_of_Candles);
                            });
                            break;
                        case UpbitCommands.주문리스트조회:
                            await _RequestCounter_Order.ProcessRequestAsync(async () =>
                            {
                                await Get_OrderList_API();
                            });
                            break;
                        case UpbitCommands.호가조회:
                            await _RequestCounter_Order.ProcessRequestAsync(async () =>
                            {
                                await Get_Orderbook_API();
                            });
                            break;
                        case UpbitCommands.지정가매도:
                            await _RequestCounter_Order.ProcessRequestAsync(async () =>
                            {
                                await Order_Sell_Limit_API(command.Parameters.CoinName, command.Parameters.Limit_Price, command.Parameters.Order_Volume);
                            });
                            break;
                        case UpbitCommands.지정가매수:
                            await _RequestCounter_Order.ProcessRequestAsync(async () =>
                            {
                                await Order_Buy_Limit_API(command.Parameters.CoinName, command.Parameters.Limit_Price, command.Parameters.Order_Volume);
                            });
                            break;
                        case UpbitCommands.미체결조회:
                            await _RequestCounter_Order.ProcessRequestAsync(async () =>
                            {
                                await Get_PendingOrders_API();
                            });
                            break;
                        case UpbitCommands.미체결취소:
                            await _RequestCounter_Order.ProcessRequestAsync(async () =>
                            {
                                await Cancel_PendingOrder_API(command.Parameters.CoinName);
                            });
                            break;
                    }
                }
                else
                {
                    await Task.Delay(100); // 명령이 없으면 대기
                }
            }
        }

        private Upbit_Command GetNextCommand()
        {
            lock (_cmdLock)
            {
                if (_CMD_Requests.Count > 0)
                {
                    var command = _CMD_Requests[0];
                    _CMD_Requests.RemoveAt(0);
                    return command;
                }
                return null;
            }
        }

        // 서버 요청 제한 핸들러
        private readonly ApiRequestHandler _RequestCounter_Order = new ApiRequestHandler(10, TimeSpan.FromSeconds(1));
        class ApiRequestHandler
        {
            private readonly int _maxRequestsPerInterval;
            private readonly TimeSpan _interval;
            private readonly SemaphoreSlim _semaphore;
            private readonly Queue<DateTime> _requestTimestamps;

            public ApiRequestHandler(int maxRequestsPerInterval, TimeSpan interval)
            {
                _maxRequestsPerInterval = maxRequestsPerInterval;
                _interval = interval;
                _semaphore = new SemaphoreSlim(maxRequestsPerInterval, maxRequestsPerInterval);
                _requestTimestamps = new Queue<DateTime>();
            }

            public async Task<bool> ProcessRequestAsync(Func<Task> apiRequest)
            {
                lock (_requestTimestamps)
                {
                    // 오래된 요청 시간 제거
                    while (_requestTimestamps.Count > 0 &&
                           (DateTime.UtcNow - _requestTimestamps.Peek()) > _interval)
                    {
                        _requestTimestamps.Dequeue();
                        _semaphore.Release();
                    }
                }

                // 요청 가능 여부 확인
                if (await _semaphore.WaitAsync(TimeSpan.FromSeconds(1)))
                {
                    try
                    {
                        lock (_requestTimestamps)
                        {
                            _requestTimestamps.Enqueue(DateTime.UtcNow);
                        }

                        // API 요청 실행
                        await apiRequest();
                        return true;
                    }
                    finally
                    {
                        // 요청 간 대기
                    }
                }

                return false;
            }
        }

        #endregion




        #region UpbitAPI Response Classes

        // 계좌 조회
        private class Upbit_Account
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


        // 코인(마켓) 리스트 (거래 가능 코인명 리스트)
        private class Upbit_Market
        {
            [JsonProperty("market")]
            public string Name_Market { get; set; }

            [JsonProperty("korean_name")]
            public string Name_KR { get; set; }

            [JsonProperty("english_name")]
            public string Name_ENG { get; set; }
        }

        // Ticker 조회
        private class Upbit_Ticker
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


        // 분 캔들 조회
        private class Upbit_Candle_Minutes
        {
            public string market { get; set; }
            public string candle_date_time_utc { get; set; }
            public string candle_date_time_kst { get; set; }
            public double opening_price { get; set; }
            public double high_price { get; set; }
            public double low_price { get; set; }
            public double trade_price { get; set; }
            public long timestamp { get; set; }
            public double candle_acc_trade_price { get; set; }
            public double candle_acc_trade_volume { get; set; }
            public int unit { get; set; }
        }


        // 주문 요청
        public class Upbit_Order
        {
            [JsonProperty("uuid")]
            public string Uuid { get; set; } // 주문의 고유 아이디

            [JsonProperty("side")]
            public string Side { get; set; } // 주문 종류

            [JsonProperty("ord_type")]
            public string OrderType { get; set; } // 주문 방식

            [JsonProperty("price")]
            public string Price { get; set; } // 주문 당시 화폐 가격 (NumberString)

            [JsonProperty("state")]
            public string State { get; set; } // 주문 상태

            [JsonProperty("market")]
            public string Market { get; set; } // 마켓의 유일키

            [JsonProperty("created_at")]
            public string CreatedAt { get; set; } // 주문 생성 시간

            [JsonProperty("volume")]
            public string Volume { get; set; } // 사용자가 입력한 주문 양 (NumberString)

            [JsonProperty("remaining_volume")]
            public string RemainingVolume { get; set; } // 체결 후 남은 주문 양 (NumberString)

            [JsonProperty("reserved_fee")]
            public string ReservedFee { get; set; } // 수수료로 예약된 비용 (NumberString)

            [JsonProperty("remaining_fee")]
            public string RemainingFee { get; set; } // 남은 수수료 (NumberString)

            [JsonProperty("paid_fee")]
            public string PaidFee { get; set; } // 사용된 수수료 (NumberString)

            [JsonProperty("locked")]
            public string Locked { get; set; } // 거래에 사용중인 비용 (NumberString)

            [JsonProperty("executed_volume")]
            public string ExecutedVolume { get; set; } // 체결된 양 (NumberString)

            [JsonProperty("trades_count")]
            public int TradesCount { get; set; } // 해당 주문에 걸린 체결 수 (Integer)

            [JsonProperty("time_in_force")]
            public string TimeInForce { get; set; } // IOC, FOK 설정

            [JsonProperty("identifier")]
            public string Identifier { get; set; } // 조회용 사용자 지정 값
        }


        // 주문 리스트 조회
        public class Upbit_OrderList
        {
            [JsonProperty("uuid")]
            public string Uuid { get; set; } // 주문 고유 아이디

            [JsonProperty("side")]
            public string Side { get; set; } // 주문 종류 (bid: 매수, ask: 매도)

            [JsonProperty("ord_type")]
            public string OrdType { get; set; } // 주문 방식 (limit, price, market, best)

            [JsonProperty("price")]
            public string Price { get; set; } // 주문 당시 화폐 가격 (NumberString)

            [JsonProperty("avg_price")]
            public string AvgPrice { get; set; } // 주문 당시 화폐 가격 (NumberString)

            [JsonProperty("state")]
            public string State { get; set; } // 주문 상태 (done: 체결 완료, cancel: 취소됨)

            [JsonProperty("market")]
            public string Market { get; set; } // 마켓 ID (예: KRW-BTC, KRW-ETH)

            [JsonProperty("created_at")]
            public string CreatedAt { get; set; } // 주문 생성 시각 (DateString)

            [JsonProperty("volume")]
            public string Volume { get; set; } // 사용자가 입력한 주문량 (NumberString)

            [JsonProperty("remaining_volume")]
            public string RemainingVolume { get; set; } // 체결 후 남은 주문량 (NumberString)

            [JsonProperty("reserved_fee")]
            public string ReservedFee { get; set; } // 수수료로 예약된 비용 (NumberString)

            [JsonProperty("remaining_fee")]
            public string RemainingFee { get; set; } // 남은 수수료 (NumberString)

            [JsonProperty("paid_fee")]
            public string PaidFee { get; set; } // 사용된 수수료 (NumberString)

            [JsonProperty("locked")]
            public string Locked { get; set; } // 거래에 사용 중인 비용 (NumberString)

            [JsonProperty("executed_volume")]
            public string ExecutedVolume { get; set; } // 체결된 양 (NumberString)

            [JsonProperty("trades_count")]
            public int TradesCount { get; set; } // 해당 주문에 걸린 체결 수 (Integer)

            [JsonProperty("time_in_force")]
            public string TimeInForce { get; set; } // IOC / FOK 설정 값 (String)

            public DateTime CreatedAt_DT { get; set; }
        }


        // 미체결 조회
        public class Upbit_PendingOrder
        {
            [JsonProperty("uuid")]
            public string Uuid { get; set; } // 주문의 고유 아이디

            [JsonProperty("side")]
            public string Side { get; set; } // 주문 종류

            [JsonProperty("ord_type")]
            public string OrderType { get; set; } // 주문 방식

            [JsonProperty("price")]
            public string Price { get; set; } // 주문 당시 화폐 가격 (NumberString)

            [JsonProperty("state")]
            public string State { get; set; } // 주문 상태

            [JsonProperty("market")]
            public string Market { get; set; } // 마켓의 유일키

            [JsonProperty("created_at")]
            public string CreatedAt { get; set; } // 주문 생성 시간

            [JsonProperty("volume")]
            public string Volume { get; set; } // 사용자가 입력한 주문 양 (NumberString)

            [JsonProperty("remaining_volume")]
            public string RemainingVolume { get; set; } // 체결 후 남은 주문 양 (NumberString)

            [JsonProperty("reserved_fee")]
            public string ReservedFee { get; set; } // 수수료로 예약된 비용 (NumberString)

            [JsonProperty("remaining_fee")]
            public string RemainingFee { get; set; } // 남은 수수료 (NumberString)

            [JsonProperty("paid_fee")]
            public string PaidFee { get; set; } // 사용된 수수료 (NumberString)

            [JsonProperty("locked")]
            public string Locked { get; set; } // 거래에 사용 중인 비용 (NumberString)

            [JsonProperty("executed_volume")]
            public string ExecutedVolume { get; set; } // 체결된 양 (NumberString)

            [JsonProperty("trades_count")]
            public int TradesCount { get; set; } // 해당 주문에 걸린 체결 수

            [JsonProperty("time_in_force")]
            public string TimeInForce { get; set; } // IOC, FOK 설정

            [JsonProperty("identifier")]
            public string Identifier { get; set; } // 조회용 사용자 지정 값
        }


        // 호가 조회
        public class Upbit_Orderbook
        {
            [JsonProperty("market")]
            public string Market { get; set; } // 종목 코드

            [JsonProperty("timestamp")]
            public long Timestamp { get; set; } // 호가 생성 시각

            [JsonProperty("total_ask_size")]
            public double TotalAskSize { get; set; } // 호가 매도 총 잔량

            [JsonProperty("total_bid_size")]
            public double TotalBidSize { get; set; } // 호가 매수 총 잔량

            [JsonProperty("orderbook_units")]
            public List<Upbit_OrderbookUnit> OrderbookUnits { get; set; } // 호가 리스트
        }


        // 호가 유닛
        public class Upbit_OrderbookUnit
        {
            [JsonProperty("ask_price")]
            public double AskPrice { get; set; } // 매도 호가

            [JsonProperty("bid_price")]
            public double BidPrice { get; set; } // 매수 호가

            [JsonProperty("ask_size")]
            public double AskSize { get; set; } // 매도 잔량

            [JsonProperty("bid_size")]
            public double BidSize { get; set; } // 매수 잔량
        }




        #endregion






        #region Events



        // 정보 업데이트
        public event EventHandler<UpbitDataEventArgs> UpbitDataRetrieved;
        public class UpbitDataEventArgs : EventArgs
        {
            public UpbitData UpbitData { get; set; }

            public UpbitDataEventArgs(UpbitData upbitData)
            {
                UpbitData = upbitData;
            }
        }



        // 주문 완료 이벤트
        public event EventHandler<OrderEventArgs> OrderRetrieved;
        public class OrderEventArgs : EventArgs
        {
            public Order Order { get; }
            public string Message { get; }

            public OrderEventArgs(Order order, string message)
            {
                Order = order;
                Message = message;
            }
        }


        #endregion





        #region Functions



        // List<Candle> to Candle Chart Series
        public Series Create_CandleSeries_from_List(List<Candle> candles)
        {
            var tempCandles = candles.ToList();

            Series _series = new Series("캔들");
            _series.ChartType = SeriesChartType.Candlestick;
            _series.XValueType = ChartValueType.DateTime;
            _series["OpenCloseStyle"] = "Triangle";
            _series["ShowOpenClose"] = "Both";
            _series.IsXValueIndexed = false;
            _series.IsVisibleInLegend = false;

            foreach (var candle in tempCandles)
            {
                var index = _series.Points.AddXY(candle.Datetime, candle.Trade_Price); // X축 값과 초기 Y값 추가

                // 캔들 차트의 시가, 종가, 저가, 고가 설정
                _series.Points[index].YValues = new double[4];
                _series.Points[index].YValues[0] = candle.Low_Price;      // Low
                _series.Points[index].YValues[1] = candle.High_Price;     // High
                _series.Points[index].YValues[2] = candle.Opening_Price;  // Open
                _series.Points[index].YValues[3] = candle.Trade_Price;    // Close
                _series.Points[index].Color = candle.Red_or_Blue == "Red" ? Color.Red : Color.Blue;
            }

            // 양봉(빨강)과 음봉(파랑) 색상 설정
            _series["PriceUpColor"] = "Red";   // 상승(양봉) 색상
            _series["PriceDownColor"] = "Blue"; // 하락(음봉) 색상

            return _series;
        }


        // List<Candle> to Bollinger Series
        public List<Series> Create_BollingerSeries_from_List(List<Candle> candles)
        {
            List<Series> series_ret = new List<Series>();

            var tempCandles = candles.ToList();

            Series series_bollinger_upper = new Series("볼린저 상단");
            series_bollinger_upper.ChartType = SeriesChartType.Line;
            series_bollinger_upper.XValueType = ChartValueType.DateTime;
            series_bollinger_upper.Color = Color.Blue;
            series_bollinger_upper.IsVisibleInLegend = false;

            Series series_bollinger_mid = new Series("볼린저 중단");
            series_bollinger_mid.ChartType = SeriesChartType.Line;
            series_bollinger_mid.XValueType = ChartValueType.DateTime;
            series_bollinger_mid.Color = Color.Green;
            series_bollinger_mid.IsVisibleInLegend = false;

            Series series_bollinger_lower = new Series("볼린저 하단");
            series_bollinger_lower.ChartType = SeriesChartType.Line;
            series_bollinger_lower.XValueType = ChartValueType.DateTime;
            series_bollinger_lower.Color = Color.Gold;
            series_bollinger_lower.IsVisibleInLegend = false;


            foreach (var candle in tempCandles)
            {
                // 볼린저 밴드
                series_bollinger_upper.Points.AddXY(candle.Datetime, candle.Bollinger_Upper);
                series_bollinger_mid.Points.AddXY(candle.Datetime, candle.Bollinger_Middle);
                series_bollinger_lower.Points.AddXY(candle.Datetime, candle.Bollinger_Lower);
            }

            series_ret.Add(series_bollinger_upper);
            series_ret.Add(series_bollinger_mid);
            series_ret.Add(series_bollinger_lower);

            return series_ret;
        }


        // List<Candle> to Peak n Trough Series
        public Series Create_PeakNtroughSeries(string SeriesName)
        {
            Series _series = new Series(SeriesName);
            _series.ChartType = SeriesChartType.Line;
            _series.XValueType = ChartValueType.DateTime;
            _series.Color = Color.Black;
            _series.IsVisibleInLegend = false;

            return _series;
        }
        


        // 오더리스트에서 수익률 계산하기
        OrderResults Create_OrderListProfits_from_OrderList(List<Upbit_OrderList> orderList)
        {
            // 오더리스트 결과
            OrderResults orderResult = new OrderResults();
            orderResult.orderResults_Trade = new List<OrderResult_by_Trade>();
            orderResult.ordereResults_Day = new List<OrderResult_by_Day>();


            // 매도 전체
            var askOrders = orderList.Where(a => a.Side == "ask").ToList();
            askOrders = askOrders.Where(a => a.State == "done").ToList();

            // 매수 전체
            var bidOrders = orderList.Where(a => a.Side == "bid").ToList();


            // 거래별 수익률 저장
            foreach (var ask in askOrders)
            {
                // 매도한 시간 이후부터 검색
                var targetBid = bidOrders
                    .Where(a => a.CreatedAt_DT <= ask.CreatedAt_DT) // 매도 이전의 매수만 검색
                    .FirstOrDefault(a => a.ExecutedVolume == ask.ExecutedVolume && a.Market == ask.Market);

                if (targetBid != null)
                {
                    decimal buyPrice = 0;
                    if (targetBid.PaidFee != "0")
                    {
                        buyPrice = (decimal.Parse(targetBid.PaidFee) / 0.0005m) + decimal.Parse(targetBid.PaidFee);
                    }
                    else
                    {
                        buyPrice = (decimal.Parse(targetBid.RemainingFee) / 0.0005m) + decimal.Parse(targetBid.RemainingFee);
                    }
                    

                    decimal sellPrice = 0;
                    if(ask.Price != null)
                    {
                        sellPrice = (decimal.Parse(ask.Price) * decimal.Parse(ask.ExecutedVolume)) - decimal.Parse(ask.PaidFee);
                    }
                    else
                    {
                        sellPrice = (decimal.Parse(ask.PaidFee) / 0.0005m) - decimal.Parse(ask.PaidFee);
                    }

                    // 수익 계산
                    decimal tempProfit = sellPrice - buyPrice;
                    decimal tempProfitRate = ((sellPrice - buyPrice) / buyPrice) * 100;

                    // 기록 후 리스트에 저장
                    OrderResult_by_Trade tempOrderTrade = new OrderResult_by_Trade
                    {
                        CoinName = ask.Market.Replace("KRW-", ""),
                        CoinNameKR = CoinListData.FirstOrDefault(c => c.CoinName == ask.Market.Replace("KRW-", ""))?.CoinNameKR,
                        BuyPrice = (double)buyPrice,
                        SellPrice = (double)sellPrice,
                        Profit = (double)tempProfit,
                        ProfitRate = (double)tempProfitRate,
                        dateTime = ask.CreatedAt_DT
                    };

                    orderResult.orderResults_Trade.Add(tempOrderTrade);
                }
            }



            var dailyResults = orderResult.orderResults_Trade
                .GroupBy(trade => trade.dateTime.Date) // 날짜별 그룹화
                .Select(group => new OrderResult_by_Day
                {
                    dateTime = group.Key,  // 해당 날짜
                    BuyPrice = group.Sum(trade => trade.BuyPrice), // 일별 총 매수 정산 금액
                    SellPrice = group.Sum(trade => trade.SellPrice), // 일별 총 매도 정산 금액
                    Profit = group.Sum(trade => trade.SellPrice) - group.Sum(trade => trade.BuyPrice), // 일별 총 수익금
                    ProfitRate = group.Sum(trade => trade.BuyPrice) > 0
                        ? ((group.Sum(trade => trade.SellPrice) - group.Sum(trade => trade.BuyPrice)) / group.Sum(trade => trade.BuyPrice)) * 100
                        : 0  // 매수 금액이 0이면 수익률 0%
                })
                .ToList();

            // 일별 수익 데이터를 저장
            orderResult.ordereResults_Day.AddRange(dailyResults);


            return orderResult;
        }



        // 지정가 소수점 맞추기
        public decimal GetAdjustedLimitPrice(string CoinName, decimal Sell_Price)
        {

            // 주문 가격 단위에 따라 매도 가격 조정
            decimal adjustedSellPrice = 0;

            // 예외 코인인지 확인
            if (_ExceptionCoins.Contains(CoinName))
            {
                adjustedSellPrice = Math.Floor(Sell_Price / 1) * 1; // 1 KRW 단위
            }
            else if (Sell_Price >= 2000000)
            {
                adjustedSellPrice = Math.Floor(Sell_Price / 1000) * 1000; // 1,000 KRW 단위
            }
            else if (Sell_Price >= 1000000)
            {
                adjustedSellPrice = Math.Floor(Sell_Price / 500) * 500; // 500 KRW 단위
            }
            else if (Sell_Price >= 500000)
            {
                adjustedSellPrice = Math.Floor(Sell_Price / 100) * 100; // 100 KRW 단위
            }
            else if (Sell_Price >= 100000)
            {
                adjustedSellPrice = Math.Floor(Sell_Price / 50) * 50; // 50 KRW 단위
            }
            else if (Sell_Price >= 10000)
            {
                adjustedSellPrice = Math.Floor(Sell_Price / 10) * 10; // 10 KRW 단위
            }
            else if (Sell_Price >= 1000)
            {
                adjustedSellPrice = Math.Floor(Sell_Price / 1) * 1; // 1 KRW 단위
            }
            else if (Sell_Price >= 100)
            {
                adjustedSellPrice = Math.Floor(Sell_Price / 0.1m) * 0.1m; // 0.1 KRW 단위
            }
            else if (Sell_Price >= 10)
            {
                adjustedSellPrice = Math.Floor(Sell_Price / 0.01m) * 0.01m; // 0.01 KRW 단위
            }
            else if (Sell_Price >= 1)
            {
                adjustedSellPrice = Math.Floor(Sell_Price / 0.001m) * 0.001m; // 0.001 KRW 단위
            }
            else if (Sell_Price >= 0.1m)
            {
                adjustedSellPrice = Math.Floor(Sell_Price / 0.0001m) * 0.0001m; // 0.0001 KRW 단위
            }
            else if (Sell_Price >= 0.01m)
            {
                adjustedSellPrice = Math.Floor(Sell_Price / 0.00001m) * 0.00001m; // 0.00001 KRW 단위
            }
            else if (Sell_Price >= 0.001m)
            {
                adjustedSellPrice = Math.Floor(Sell_Price / 0.000001m) * 0.000001m; // 0.000001 KRW 단위
            }
            else
            {
                adjustedSellPrice = Math.Floor(Sell_Price / 0.0000001m) * 0.0000001m; // 0.0000001 KRW 단위
            }

            return adjustedSellPrice;
        }

        // 예외처리 코인 리스트
        List<string> _ExceptionCoins = new List<string>();
        void Init_ExceptionCoins()
        {
            _ExceptionCoins.Add("ADA");
            _ExceptionCoins.Add("ALGO");
            _ExceptionCoins.Add("BLUR");
            _ExceptionCoins.Add("CELO");
            _ExceptionCoins.Add("ELF");
            _ExceptionCoins.Add("EOS");
            _ExceptionCoins.Add("GRS");
            _ExceptionCoins.Add("GRT");
            _ExceptionCoins.Add("ICX");
            _ExceptionCoins.Add("MANA");
            _ExceptionCoins.Add("MINA");
            _ExceptionCoins.Add("POL");
            _ExceptionCoins.Add("SAND");
            _ExceptionCoins.Add("SEI");
            _ExceptionCoins.Add("STG");
            _ExceptionCoins.Add("TRX");
        }


        #endregion


    }
}
