using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
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
        private string _tradeHistoryPath;
        public Upbit_API_v2(string AccessKey, string SecretKey, string TradeHistoryPath)
        {
            _accessKey = AccessKey;
            _secretKey = SecretKey;

            // 거래기록 저장위치
            _tradeHistoryPath = TradeHistoryPath;


            // 기본정보 초기화
            Init_Info();

            // 예외처리 코인 등록
            Init_ExceptionCoins();


            // 업데이트 타임
            update_time_ms = 1000;

            // 데이터 업데이터
            _bworker_DataUpdator.DoWork += _bworker_DataUpdator_DoWork;
            _bworker_DataUpdator.RunWorkerAsync();
        }


        #region 기본정보 및 캔들 정보

        private Info _Info = new Info();    // 기본 정보
        public List<CoinList> CoinListData = new List<CoinList>();
        private void Init_Info()
        {
            _Info.TargetCoinName = "1INCH";
            _Info.Candle_Minutes = 10;
            _Info.Candle_N_of_Candles = 60;
        }

        public void Set_TargetCoin_CandleInfo(Info Info)
        {
            _Info.TargetCoinName = Info.TargetCoinName;
            _Info.TargetCoinNameKR = CoinListData.FirstOrDefault(a => a.CoinName == Info.TargetCoinName).CoinNameKR;
            _Info.Candle_Minutes = Info.Candle_Minutes;
            _Info.Candle_N_of_Candles = Info.Candle_N_of_Candles;
        }

        #endregion





        #region Data Classes


        // 정보 업데이트. (계좌, Ticker, Candle, Orderbook, OrderList)
        public class UpbitData
        {
            public Info Info { get; set; }
            public List<CoinList> CoinLists { get; set; }
            public List<Ticker> Tickers { get; set; }
            public List<Account> Accounts { get; set; }
            public CandleData CandleData { get; set; }
            public List<PendingOrder> PendingOrders { get; set; }
            public List<Orderbook> Orderbooks { get; set; }
            public Search_Data Search_Data { get; set; }
            public TodayTradesResults Today_TradeResult {get; set;}
        }



        public class Info
        {
            public int KRW_Total { get; set; }               // 총 자산
            public int KRW_Orderable { get; set; }           // 주문 가능 원화
            public int KRW_Ordering { get; set; }            // 주문중인 원화

            public int Eval_Profit { get; set; }            // 평가손익
            public double Eval_TotalProfit { get; set; }    // 평가손익 합
            public int Eval_TotalPrice { get; set; }        // 평가금액 합
            public double Eval_ProfitRate {  get; set; }    // 평가손익 수익률

            public string Total_CoinPrice { get; set; }         // 총 매수 코인 금액

            public double ChangeRate_BitCoin { get; set; }      // 비트코인 변화율

            public int N_of_RISE { get; set; }                  // 상승 코인 수
            public int N_of_FALL { get; set; }                  // 하락 코인 수
            public int N_of_EVEN { get; set; }                  // 하락 코인 수


            public string TargetCoinName { get; set; }
            public string TargetCoinNameKR { get; set; }

            // 분 캔들 관련
            public int Candle_Minutes { get; set; }
            public int Candle_N_of_Candles { get; set; }
        }


        List<Account> _Recent_Accounts = new List<Account>();

        // 계좌 정보
        public class Account
        {
            public string CoinName { get; set; }        // 코인명 영문
            public string CoinNameKR { get; set; }      // 코인명 한글
            public string Volume { get; set; }          // 보유 볼륨
            public string Volume_Pending { get; set; }  // 미체결 볼륨
            public double Volume_Double { get; set; }   // volume이든 pending이든 double로
            public string AvgBuyPrice { get; set; }     // 매수평균가
            public decimal TotalBoughtPrice { get; set; } // 총 매수가

            public double CurrentPrice { get; set; }   // 현재가
            public double Profit { get; set; }         // 수익
            public double ProfitRate {  get; set; }    // 수익률

            public double EvalProfit { get; set; }      // 평가손익

            public DateTime DateTime_Bought { get; set; }      // 매수날짜
        }

        // 코인 리스트
        public class CoinList
        {
            public string CoinName { get; set; }        // 코인명 영문
            public string CoinNameKR { get; set; }      // 코인명 한글
            public bool IsWarning { get; set; }         // 유의 정보
            public string Caution { get; set; }         // 주의 정보
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


            public double Acc_Trade_Price_24h { get; set; }     // 24시간 누적 거래대금
            public double highest_52_week_price { get; set; }   // 52주 신고가
            public double lowest_52_week_price { get; set; }   // 52주 신저가
        }

        // 주문 결과
        public class Order
        {
            public string CoinName { get; set; }        // 코인명 영문
            public string CoinNameKR { get; set; }      // 코인명 한글

            public string Type {  get; set; }                    // 매수, 매도
            public double Buying_Target_Price {  get; set; }     // 매수 금액
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


            // 추세선
            public Series upperTrendSeries { get; set; }
            public Series lowerTrendSeries { get; set; }
            public string TrendPattern { get; set; }

            // 일봉
            public List<Candle> Candles_Day { get; set; }
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

            public double MA {  get; set; }                 // 이동평균선
            public double Bollinger_Upper { get; set; }     // 볼린저 상단
            public double Bollinger_Middle { get; set; }    // 볼린저 중단
            public double Bollinger_Lower { get; set; }     // 볼린저 하단

            public string Red_or_Blue { get; set; }         // 양봉 음봉 여부

            // 봉우리, 골
            public double Peak {  get; set; }
            public double Trough {  get; set; }

            // 추세선
            public double UpperTrendLine { get; set; }  // 상단 추세선 값
            public double LowerTrendLine { get; set; }  // 하단 추세선 값
        }



        // 일별 거래 손익 (하루의 모든 매도 기록을 포함)
        public class Trade_Profits_Days
        {
            public DateTime Date { get; set; }  // 하루 기준 날짜
            public List<Trade_Profits> Trade_Profits { get; set; } = new List<Trade_Profits>();  // 해당 날짜의 모든 매도 기록
            public decimal TotalProfit => Trade_Profits.Sum(t => t.Profit);  // 하루 총 수익
        }

        // 개별 매도 거래 손익 (해당 날짜 내 코인별 매도 데이터)
        public class Trade_Profits
        {
            public string CoinName { get; set; }    // 코인명 (영문)
            public string CoinNameKR { get; set; }  // 코인명 (한글)
            public decimal Profit { get; set; }     // 개별 매도 손익
        }



        // 주문 거래 내역
        public class Trade_History
        {
            public string CoinName { get; set; }        // 코인명 영문
            public string CoinNameKR { get; set; }      // 코인명 한글

            public DateTime DateTime_CreatedAt { get; set; }     // 주문 체결 시간

            public string Side { get; set; }        // 매수, 매도

            public decimal Executed_Volume { get; set; }     // 체결양
            public decimal Evaluated_Funds { get; set; }     // 정산 금액
        }


        // 오늘 거래
        public class TodayTradesResults
        {
            public int Profit_cnt { get; set; }
            public int Loss_cnt { get; set; }

            public decimal Profit_Sum { get; set; }
            public decimal Loss_Sum { get; set; }
            public decimal ProfitLoss_Sum { get; set; }

            public List<Trade_Profits> Trade_Profits { get; set; }

            public bool IsProfitChanged { get; set; }
            public int ChangedProfit { get; set; }
        }





        // 호가
        public class Orderbook
        {
            public double Price { get; set; }
            public double Volume { get; set; }
            public double Percent { get; set; }
            public double ChangeRate { get; set; }
        }


        List<PendingOrder> _Recent_PendingOrders = new List<PendingOrder>();

        // 미체결 주문
        public class PendingOrder
        {
            public string CoinName { get; set; }        // 코인명 영문
            public string CoinNameKR { get; set; }      // 코인명 한글

            public string Uuid { get; set; } // 주문의 고유 아이디

            public string OrderType { get; set; }   // 주문 종류. 매수, 매도

            public double Selling_Reserved_Rate { get; set; }  // 매도 예약 퍼센트

            public double Buying_Reserved_Price { get; set; }    // 매수 예약 가격

            public bool IsSellingPart { get; set; }             // 부분 매도 여부
        }

        #endregion



        #region Order Public Methods

        List<string> _List_Orders = new List<string>();
        
        // 시장가 매수
        public void Request_Buy_Market(string Price)
        {
            // 보유중인 미체결이 있거나, 매수 예약이 있음.
            var pendingOrder = _Recent_PendingOrders.FirstOrDefault(a => a.CoinName == _Info.TargetCoinName);
            if (pendingOrder != null)
            {
                string uuid = pendingOrder.Uuid;
                string tempOrder = $"미체결취소,{uuid}";
                _List_Orders.Add(tempOrder);
            }

            string tempOrder2 = $"시장가매수,{_Info.TargetCoinName},{Price}";

            _List_Orders.Add(tempOrder2);
        }

        // 시장가 매도
        public void Request_Sell_Market()
        {

            // 계좌에 있는지 확인
            var account = _Recent_Accounts.FirstOrDefault(a => a.CoinName == _Info.TargetCoinName);
            if (account != null)
            {
                // 보유중인 미체결이 있거나, 매수 예약이 있음.
                var pendingOrder = _Recent_PendingOrders.FirstOrDefault(a => a.CoinName == _Info.TargetCoinName);
                if (pendingOrder != null)
                {
                    string uuid = pendingOrder.Uuid;
                    string tempOrder = $"미체결취소,{uuid}";
                    _List_Orders.Add(tempOrder);
                }

                string tempOrder2 = $"시장가매도,{_Info.TargetCoinName},{account.Volume}";
                _List_Orders.Add(tempOrder2);
            }
        }


        // 지정가 매도 주문
        public void Request_Sell_Limit(string CoinName, string LimitPrice)
        {
            // 계좌에 있는지 확인
            var account = _Recent_Accounts.FirstOrDefault(a => a.CoinName == CoinName);
            if (account != null)
            {
                string volume = "";

                // 보유중인 미체결이 있거나, 매수 예약이 있음.
                var pendingOrder = _Recent_PendingOrders.FirstOrDefault(a => a.CoinName == CoinName);
                if (pendingOrder != null)
                {
                    string uuid = pendingOrder.Uuid;
                    string tempOrder = $"미체결취소,{uuid}";
                    _List_Orders.Add(tempOrder);

                    volume = account.Volume_Pending;
                }
                else
                {
                    volume = account.Volume;
                }

                string tempOrder2 = $"지정가매도,{CoinName},{LimitPrice},{volume}";
                _List_Orders.Add(tempOrder2);
            }
        }

        // 지정가 매도 주문 부분만
        public void Request_Sell_Limit_Ratio(string CoinName, string LimitPrice, double Ratio)
        {
            // 계좌에 있는지 확인
            var account = _Recent_Accounts.FirstOrDefault(a => a.CoinName == CoinName);
            if (account != null)
            {
                double volume = 0;

                // 보유중인 미체결이 있거나, 매수 예약이 있음.
                var pendingOrder = _Recent_PendingOrders.FirstOrDefault(a => a.CoinName == CoinName);
                if (pendingOrder != null)
                {
                    string uuid = pendingOrder.Uuid;
                    string tempOrder = $"미체결취소,{uuid}";
                    _List_Orders.Add(tempOrder);

                    volume = double.Parse(account.Volume_Pending);
                }
                else
                {
                    volume = double.Parse(account.Volume);
                }

                // 볼륨 조정
                volume = AdjustVolume(volume, Ratio);

                string tempOrder2 = $"지정가매도,{CoinName},{LimitPrice},{volume}";
                _List_Orders.Add(tempOrder2);
            }
        }


        // 지정가 매수 주문
        public void Request_Buy_Limit(string CoinName, string LimitPrice, string BuyingPrice)
        {
            decimal buyingPrice = decimal.Parse(BuyingPrice);
            decimal targetPrice = decimal.Parse(LimitPrice);
            string volume = (buyingPrice / targetPrice).ToString();

            // 보유중인 미체결이 있거나, 매수 예약이 있음.
            var pendingOrder = _Recent_PendingOrders.FirstOrDefault(a => a.CoinName == CoinName);
            if (pendingOrder != null)
            {
                string uuid = pendingOrder.Uuid;
                string tempOrder = $"미체결취소,{uuid}";
                _List_Orders.Add(tempOrder);
            }

            string tempOrder2 = $"지정가매수,{CoinName},{LimitPrice},{volume}";
            _List_Orders.Add(tempOrder2);
        }


        
        // 미체결 취소
        public void Request_Cancel_PendingOrder(string CoinName)
        {
            // 보유중인 미체결이 있거나, 매수 예약이 있음.
            var pendingOrder = _Recent_PendingOrders.FirstOrDefault(a => a.CoinName == CoinName);
            if (pendingOrder != null)
            {
                string uuid = pendingOrder.Uuid;
                string tempOrder = $"미체결취소,{uuid}";
                _List_Orders.Add(tempOrder);
            }
        }



        

        #endregion





        #region Data Update Background Worker


        // 정해진 시간마다 데이터 업데이트 (ticker, account 등)
        BackgroundWorker _bworker_DataUpdator = new BackgroundWorker();
        int update_time_ms = 1000;
        private async void _bworker_DataUpdator_DoWork(object sender, DoWorkEventArgs e)
        {
            // 코인명 업데이트
            await Get_CoinList_API();

            Stopwatch stopwatch = new Stopwatch();

            // 전체 매수 코인 금액
            decimal _TotalBuyPrice = 0;
            List<Account> temp_accounts = await Get_Accounts_API();
            _TotalBuyPrice = Calc_Total_Buyed_Price(temp_accounts);


            // 전체 코인 마켓 정보 업데이트 매 30초마다 한번씩
            int marketList_Update_Counter = 30;


            // 어제 데이터 저장하기
            TradeHistory_Save_as_File_YesterDay();
            Thread.Sleep(1000);


            // 지난 거래 기록 불러오기
            LoadAllTradeHistory(_tradeHistoryPath);


            while (true)
            {
                stopwatch.Restart();  // 시작 시간 기록



                // 주문이 있나 ?
                if (_List_Orders.Count != 0)
                {
                    // 주문 내용
                    string[] command = _List_Orders.First().Split(',');


                    switch (command[0])
                    {
                        // 시장가 매수
                        case "시장가매수":
                            if (await Order_Buy_Market_API(command[1], command[2]))
                            {
                                OrderRetrieved?.Invoke(this, new OrderEventArgs($"시장가매수,성공,{command[1]}"));
                            }
                            else
                            {
                                OrderRetrieved?.Invoke(this, new OrderEventArgs($"시장가매수,실패,{command[1]}"));
                            }
                            break;


                        // 시장가 매도
                        case "시장가매도":
                            if (await Order_Sell_Market_API(command[1], command[2]))
                            {
                                OrderRetrieved?.Invoke(this, new OrderEventArgs($"시장가매도,성공,{command[1]}"));
                            }
                            else
                            {
                                OrderRetrieved?.Invoke(this, new OrderEventArgs($"시장가매도,실패,{command[1]}"));
                            }
                            break;


                        // 지정가 매수
                        case "지정가매수":
                            if (await Order_Buy_Limit_API(command[1], command[2], command[3]))
                            {
                                OrderRetrieved?.Invoke(this, new OrderEventArgs($"지정가매수,성공,{command[1]}"));
                            }
                            else
                            {
                                OrderRetrieved?.Invoke(this, new OrderEventArgs($"지정가매수,실패,{command[1]}"));
                            }
                            break;


                        // 지정가 매도
                        case "지정가매도":
                            if (await Order_Sell_Limit_API(command[1], command[2], command[3]))
                            {
                                OrderRetrieved?.Invoke(this, new OrderEventArgs($"지정가매도,성공,{command[1]}"));
                            }
                            else
                            {
                                OrderRetrieved?.Invoke(this, new OrderEventArgs($"지정가매도,실패,{command[1]}"));
                            }
                            break;


                        // 미체결 취소
                        case "미체결취소":
                            await Cancel_PendingOrder_API(command[1]);
                            break;

                    }


                    // 주문 삭제
                    _List_Orders.RemoveAt(0);


                    // 주문 완료
                    if (_List_Orders.Count == 0)
                    {
                        OrderRetrieved?.Invoke(this, new OrderEventArgs($"모든주문완료"));
                    }
                }

                // 주문 없음. 정보 업데이트
                else
                {

                    // 미리 정보 가져오기
                    string targetCoinName = _Info.TargetCoinName;
                    int candle_minutes = _Info.Candle_Minutes;
                    int candle_counts = _Info.Candle_N_of_Candles;


                    // 전체 코인 마켓 정보 업데이트 매 30초마다 한번씩만
                    marketList_Update_Counter--;
                    if (marketList_Update_Counter == 0)
                    {
                        marketList_Update_Counter = 30;

                        await Get_CoinList_API();
                    }
                    else
                    {
                        // 코인 탐색
                        CoinSearch_Processing();
                    }


                    // Upbit Data Class
                    UpbitData tempUpbitData = new UpbitData();


                    // Info
                    Info info = new Info();

                    // Ticker 정보
                    List<Ticker> tickers = await Get_Ticker_API();

                    // 계좌 정보
                    List<Account> accounts = await Get_Accounts_API();


                    if ((tickers != null) && (accounts != null))
                    {
                        // Info에 KRW넣고 KRW는 계좌에서 제거
                        var KRW_account = accounts.FirstOrDefault(a => a.CoinName == "KRW");
                        info.KRW_Orderable = (int)double.Parse(KRW_account.Volume);
                        info.KRW_Ordering = (int)double.Parse(KRW_account.Volume_Pending);
                        info.KRW_Total = (int)accounts.Sum(a => a.TotalBoughtPrice) + info.KRW_Orderable + info.KRW_Ordering;
                        accounts.RemoveAll(a => a.CoinName == "KRW");


                        // 평가손익
                        info.Eval_Profit = 0;
                        foreach (var account in accounts)
                        {
                            // 평가금액
                            double EvalPrice = tickers.FirstOrDefault(a => a.CoinNameKR == account.CoinNameKR).Trade_Price * account.Volume_Double;

                            // 평가손익
                            account.EvalProfit = EvalPrice - (double)account.TotalBoughtPrice;

                            // 전체 평가 금액 및 평가손익 합
                            info.Eval_TotalPrice += (int)EvalPrice;
                            info.Eval_TotalProfit += account.EvalProfit;
                        }


                        // 평가손익 수익률 계산
                        int totlaBuySum = (int)accounts.Sum(a => a.TotalBoughtPrice);
                        info.Eval_ProfitRate = (((double)info.Eval_TotalPrice - (double)totlaBuySum) / (double)totlaBuySum) * 100.0;


                        info.TargetCoinName = targetCoinName;
                        info.TargetCoinNameKR = CoinListData.FirstOrDefault(a => a.CoinName == targetCoinName).CoinNameKR;
                        info.Candle_Minutes = candle_minutes;
                        info.Candle_N_of_Candles = candle_counts;
                        info.N_of_RISE = tickers.FindAll(rise => rise.Change == "상승").Count;
                        info.N_of_EVEN = tickers.FindAll(rise => rise.Change == "보합").Count;
                        info.N_of_FALL = tickers.FindAll(rise => rise.Change == "하락").Count;
                        info.ChangeRate_BitCoin = tickers.FirstOrDefault(b => b.CoinNameKR == "비트코인").Change_Rate;
                        info.Total_CoinPrice = Calc_Total_Evaluated_Price(accounts.ToList(), tickers.ToList()).ToString();

                        // 총 매수 금액이 바뀌었음. 보유 코인 변화
                        decimal current_totalbuyPrice = Calc_Total_Buyed_Price(accounts.ToList());
                        if (_TotalBuyPrice != current_totalbuyPrice)
                        {
                            // 새로 생긴 코인명 찾기
                            var NewCoins = accounts.Where(a => !_Recent_Accounts.Any(b => b.CoinName == a.CoinName)).ToList();

                            // 보유 수량이 더 많아진 코인명 찾기
                            var increasedVolume = accounts.Where(a => _Recent_Accounts.Any(b => b.CoinName == a.CoinName && a.Volume_Double > b.Volume_Double)).ToList();

                            string buyCoinList = "";

                            // 새로 생긴 코인 리스트
                            foreach (var newcoin in NewCoins)
                            {
                                buyCoinList += newcoin.CoinName + ",";
                            }

                            // 기존 코인 추가 매수 리스트
                            foreach (var incVolCoin in increasedVolume)
                            {
                                buyCoinList += incVolCoin.CoinName + ",";
                            }


                            // 이벤트 발생.
                            if (buyCoinList != "")
                            {
                                OrderRetrieved?.Invoke(this, new OrderEventArgs($"매수완료,{buyCoinList}"));
                            }


                            // 갱신
                            _TotalBuyPrice = current_totalbuyPrice;
                        }


                        // 최근 계좌 정보 저장
                        _Recent_Accounts = accounts.ToList();


                        // 계좌에 수익률 계산 및 현재가 넣기
                        foreach (var account in accounts)
                        {
                            double avgBuyPrice = double.Parse(account.AvgBuyPrice);
                            double currentPrice = tickers.FirstOrDefault(a => a.CoinName == account.CoinName).Trade_Price;                  // 현재가
                            double volume = account.Volume != "0" ? double.Parse(account.Volume) : double.Parse(account.Volume_Pending);    // 보유수량
                            double profit = (currentPrice * volume) - (avgBuyPrice * volume);           // 평가손익
                            double profitrate = ((currentPrice - avgBuyPrice) / avgBuyPrice) * 100;     // 수익률

                            // 저장
                            account.Profit = profit;
                            account.ProfitRate = profitrate;
                            account.CurrentPrice = currentPrice;
                        }


                        // 캔들 업데이트
                        CandleData candleData;

                        // 분봉
                        if (candle_minutes <= 240)
                        {
                            candleData = await Get_Candle_Minutes_API(targetCoinName, candle_minutes, candle_counts);
                        }
                        // 일봉
                        else
                        {
                            candleData = await Get_Candle_Days_API(targetCoinName, candle_counts);
                        }



                        // 금일 거래내역 업데이트
                        TodayTradesResults temp_tradeResult = await Update_Today_Trades(info.KRW_Total);


                        // 미체결 업데이트
                        List<PendingOrder> pendingOrders = await Get_PendingOrders_API(accounts.ToList());

                        // 최근 미체결 내역 업데이트
                        _Recent_PendingOrders = pendingOrders.ToList();


                        // 호가 업데이트
                        //List<Orderbook> orderBooks = await Get_Orderbook_API(targetCoinName, tickers.FirstOrDefault(a => a.CoinName == targetCoinName).Trade_Price, tickers.FirstOrDefault(a => a.CoinName == targetCoinName).Change_Rate);


                        // 계좌정보에 구매 시점 저장하기
                        /*
                        foreach(var account in accounts)
                        {
                            // 해당 코인 매수 거래가 오늘 있었는지 ? 
                            var trade_today = Today_Trade_History.FirstOrDefault(a => (a.CoinNameKR == account.CoinNameKR) && (a.Side == "매수"));

                            if(trade_today != null)
                            {
                                // 매수 시점 기록
                                account.DateTime_Bought = trade_today.DateTime_CreatedAt;
                            }
                            else
                            {
                                // 해당 코인 매수가 어제 이전인지 ?
                                var trade_prev = Last_Trade_History.FirstOrDefault(a => (a.CoinNameKR == account.CoinNameKR) && (a.Side == "매수"));

                                if(trade_prev != null)
                                {
                                    // 매수 시점 기록
                                    account.DateTime_Bought = trade_prev.DateTime_CreatedAt;
                                }
                            }
                        }
                        */




                        // 정보 업데이트 후 이벤트 발생
                        tempUpbitData.Info = info;
                        tempUpbitData.CoinLists = CoinListData;
                        tempUpbitData.Tickers = tickers.ToList();
                        tempUpbitData.Accounts = accounts.ToList();
                        tempUpbitData.CandleData = candleData;
                        tempUpbitData.PendingOrders = pendingOrders;
                        //tempUpbitData.Orderbooks = orderBooks.ToList();
                        tempUpbitData.Search_Data = _Search_Data;
                        tempUpbitData.Today_TradeResult = temp_tradeResult;

                        UpbitDataRetrieved?.Invoke(this, new UpbitDataEventArgs(tempUpbitData));

                    }
                }

                stopwatch.Stop();

                // 실행 시간 고려하여 정확히 1초 주기 유지
                int elapsed = (int)stopwatch.ElapsedMilliseconds;
                int remainingTime = update_time_ms - elapsed;
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
        private async Task<List<Account>> Get_Accounts_API()
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
                List<Account> tempAccounts = new List<Account>();

                // 기본 내용 옮기기
                tempAccounts = accounts
                    .Select(s => new Account {
                        CoinName = s.Currency,
                        CoinNameKR = s.Currency != "KRW" ? CoinListData.FirstOrDefault(c => c.CoinName == s.Currency).CoinNameKR : "",
                        Volume = s.Balance, 
                        Volume_Pending = s.Locked, 
                        AvgBuyPrice = s.AvgBuyPrice })
                    .ToList();
    

                // volume double 계산
                foreach(var acc in tempAccounts)
                {

                    double temp_vol = acc.Volume != "0" ? double.Parse(acc.Volume) : 0;
                    double temp_vol_pending = acc.Volume_Pending != "0" ? double.Parse(acc.Volume_Pending) : 0;

                    acc.Volume_Double = temp_vol + temp_vol_pending;

                    // 총 매수액
                    acc.TotalBoughtPrice = (decimal)acc.Volume_Double * decimal.Parse(acc.AvgBuyPrice);
                    
                }

                


                // Deep Copy
                List<Account> ret = tempAccounts.Select(a => new Account
                {
                    CoinName = a.CoinName,
                    CoinNameKR = a.CoinNameKR,
                    Volume = a.Volume,
                    Volume_Pending = a.Volume_Pending,
                    Volume_Double = a.Volume_Double,
                    TotalBoughtPrice = a.TotalBoughtPrice,
                    AvgBuyPrice = a.AvgBuyPrice,
                    CurrentPrice = a.CurrentPrice,
                    Profit = a.Profit,
                    ProfitRate = a.ProfitRate
                }
                ).ToList();


                return ret.ToList();
            }
            else
            {
                Console.WriteLine($"API 요청 실패: {response.StatusCode} - {response.Content}");
                return null;
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

            var client = new RestClient("https://api.upbit.com/v1/market/all?is_details=true");
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
                        IsWarning = coin.Market_Event.Warning,
                        Caution = coin.Market_Event.Caution.PriceFluctuations == true ? "가격 급등락" :
                                    coin.Market_Event.Caution.TradingVolumeSoaring == true ? "거래량 급등" :
                                    coin.Market_Event.Caution.DepositAmountSoaring == true ? "입급량 급등" :
                                    coin.Market_Event.Caution.GlobalPriceDifferences == true ? "글로벌 시세 차이" :
                                    coin.Market_Event.Caution.ConcentrationOfSmallAccounts == true ? "소수 계정 집중" : ""
                    })
                    .OrderBy(coin => coin.CoinNameKR) // Name_KR 기준으로 정렬
                    .ToList();            }
            else
            {
                Console.WriteLine($"API 요청 실패: {response.StatusCode} - {response.Content}");
            }
        }


        // Ticker 조회
        private async Task<List<Ticker>> Get_Ticker_API()
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
                try
                {

                    // JSON 데이터를 List<TickerData> 형식으로 역직렬화
                    //var allTickers = JsonConvert.DeserializeObject<List<Upbit_Ticker>>(response.Content);


                    string json = response.Content;

                    // 숫자형 null → 0
                    json = json.Replace(":null", ":0");

                    // 문자열형 null → ""
                    json = Regex.Replace(json, ":null(?=\\s*[,}])", ":\"\"");

                    // 이제 DeserializeObject 해도 예외 없음
                    var allTickers = JsonConvert.DeserializeObject<List<Upbit_Ticker>>(json);


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
                            Change_Rate = ticker.SignedChangeRate * 100,
                            Trade_Volume = ticker.TradeVolume,
                            Datetime = DateTimeOffset.FromUnixTimeMilliseconds(ticker.Timestamp).UtcDateTime.AddHours(9),

                            Acc_Trade_Price_24h = ticker.AccTradePrice24h,
                            highest_52_week_price = ticker.Highest52WeekPrice,
                            lowest_52_week_price = ticker.Lowest52WeekPrice

                        })
                        .OrderBy(ticker => ticker.CoinNameKR) // Market 기준으로 정렬
                        .ToList();



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
                        Datetime = a.Datetime,

                        Acc_Trade_Price_24h = a.Acc_Trade_Price_24h,
                        highest_52_week_price = a.highest_52_week_price,
                        lowest_52_week_price = a.lowest_52_week_price
                    }
                    ).ToList();


                    return ret.ToList();
                }
                catch
                {
                    return null;
                }
                
            }
            else
            {
                Console.WriteLine($"API 요청 실패: {response.StatusCode} - {response.Content}");
                return null;
            }

        }

        // 분 캔들 조회
        private async Task<CandleData> Get_Candle_Minutes_API(string CoinName, int Minute, int N_of_Candles)
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

                string coinNameKR = CoinListData.FirstOrDefault(a => a.CoinName == CoinName).CoinNameKR;

                // Deep Copy
                List<Candle> candle_ret = tempCandles.Select(a => new Candle
                {
                    CoinName = CoinName,
                    CoinNameKR = coinNameKR,

                    Minutes = a.unit,
                    Datetime = DateTime.ParseExact(a.candle_date_time_kst, "yyyy-MM-dd'T'HH:mm:ss", null),

                    Opening_Price = a.opening_price,
                    High_Price = a.high_price,
                    Low_Price = a.low_price,
                    Trade_Price = a.trade_price,
                }
                ).ToList();



                for (int i = 0; i < candle_ret.Count; i++)
                {
                    if (i + 1 >= 20)
                    {
                        // 이동평균 계산
                        var subset = candle_ret.Skip(i + 1 - 20).Take(20);
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
                candle_ret = candle_ret.Skip(20 - 1).ToList();

                // 요청 데이터 개수만큼만 최근기준으로 남기기
                candle_ret = candle_ret.Skip(candle_ret.Count - N_of_Candles).Take(N_of_Candles).ToList();


                try
                {
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




                    CandleData tempCandleData = new CandleData();
                    tempCandleData.Candles = candle_ret.ToList();
                    tempCandleData.candleSeries = candleSeries;
                    tempCandleData.bollingerSeries = bollingerSeries.ToList();
                    tempCandleData.ChartArea = chartArea;
                    //tempCandleData.Peaks = middlePeaks.ToList();
                    //tempCandleData.Troughs = middleTroughs.ToList();
                    //tempCandleData.peakSeries = peakSeries;
                    //tempCandleData.troughSeries = troughSeries;
                    //tempCandleData.upperTrendSeries = upperTrendSeries;
                    //tempCandleData.lowerTrendSeries = lowerTrendSeries;
                    //tempCandleData.TrendPattern = trendPattern;

                    return tempCandleData;
                }
                catch
                {
                    return null;
                }
                
            }
            else
            {
                Console.WriteLine($"API 요청 실패: {response.StatusCode} - {response.Content}");
                return null;
            }
        }


        // 일봉 캔들 조회
        private async Task<CandleData> Get_Candle_Days_API(string CoinName, int N_of_Candles)
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
            var client = new RestClient($"https://api.upbit.com/v1/candles/days/");
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

                string coinNameKR = CoinListData.FirstOrDefault(a => a.CoinName == CoinName).CoinNameKR;

                // Deep Copy
                List<Candle> candle_ret = tempCandles.Select(a => new Candle
                {
                    CoinName = CoinName,
                    CoinNameKR = coinNameKR,

                    Minutes = a.unit,
                    Datetime = DateTime.ParseExact(a.candle_date_time_kst, "yyyy-MM-dd'T'HH:mm:ss", null),

                    Opening_Price = a.opening_price,
                    High_Price = a.high_price,
                    Low_Price = a.low_price,
                    Trade_Price = a.trade_price,
                }
                ).ToList();



                for (int i = 0; i < candle_ret.Count; i++)
                {
                    if (i + 1 >= 20)
                    {
                        // 이동평균 계산
                        var subset = candle_ret.Skip(i + 1 - 20).Take(20);
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
                candle_ret = candle_ret.Skip(20 - 1).ToList();

                // 요청 데이터 개수만큼만 최근기준으로 남기기
                candle_ret = candle_ret.Skip(candle_ret.Count - N_of_Candles).Take(N_of_Candles).ToList();


                try
                {
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




                    CandleData tempCandleData = new CandleData();
                    tempCandleData.Candles = candle_ret.ToList();
                    tempCandleData.candleSeries = candleSeries;
                    tempCandleData.bollingerSeries = bollingerSeries.ToList();
                    tempCandleData.ChartArea = chartArea;
                    //tempCandleData.Peaks = middlePeaks.ToList();
                    //tempCandleData.Troughs = middleTroughs.ToList();
                    //tempCandleData.peakSeries = peakSeries;
                    //tempCandleData.troughSeries = troughSeries;
                    //tempCandleData.upperTrendSeries = upperTrendSeries;
                    //tempCandleData.lowerTrendSeries = lowerTrendSeries;
                    //tempCandleData.TrendPattern = trendPattern;

                    return tempCandleData;
                }
                catch
                {
                    return null;
                }

            }
            else
            {
                Console.WriteLine($"API 요청 실패: {response.StatusCode} - {response.Content}");
                return null;
            }
        }

        // 시장가 매수 요청
        private async Task<bool> Order_Buy_Market_API(string CoinName, string Price)
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
                return true;
                //OrderRetrieved?.Invoke(this, new OrderEventArgs(order, "주문 성공"));
            }
            else
            {
                Console.WriteLine($"주문 요청 실패: {response.StatusCode} - {response.Content}");
                return false;
                // 실패 시 이벤트 발생
                //OrderRetrieved?.Invoke(this, new OrderEventArgs(null, "주문 실패"));
            }
        }

        // 시장가 매도 요청
        private async Task<bool> Order_Sell_Market_API(string CoinName, string Volume)
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
                return true;
                //OrderRetrieved?.Invoke(this, new OrderEventArgs(order, "주문 성공"));
            }
            else
            {
                Console.WriteLine($"주문 요청 실패: {response.StatusCode} - {response.Content}");
                return false;
                // 실패 시 이벤트 발생
                //OrderRetrieved?.Invoke(this, new OrderEventArgs(null, "주문 실패"));
            }
        }


        // 지정가 매도 요청
        private async Task<bool> Order_Sell_Limit_API(string CoinName, string LimitPrice, string Volume)
        {
            // 소수점 맞추기
            string adjustedPrice = GetAdjustedLimitPrice(decimal.Parse(LimitPrice)).ToString();

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
                return true;
                //OrderRetrieved?.Invoke(this, new OrderEventArgs(order, "주문 성공"));
            }
            else
            {
                Console.WriteLine($"주문 요청 실패: {response.StatusCode} - {response.Content}");
                return false;
                // 실패 시 이벤트 발생
                //OrderRetrieved?.Invoke(this, new OrderEventArgs(null, "주문 실패"));
            }
        }



        // 지정가 매수 요청
        private async Task<bool> Order_Buy_Limit_API(string CoinName, string LimitPrice, string Volume)
        {
            // 소수점 맞추기
            string adjustedPrice = GetAdjustedLimitPrice(decimal.Parse(LimitPrice)).ToString();


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
                return true;
                //OrderRetrieved?.Invoke(this, new OrderEventArgs(order, "주문 성공"));
            }
            else
            {
                Console.WriteLine($"주문 요청 실패: {response.StatusCode} - {response.Content}");
                return false;
                // 실패 시 이벤트 발생
                //OrderRetrieved?.Invoke(this, new OrderEventArgs(null, "주문 실패"));
            }
        }




        // 주문 리스트 조회
        private async Task<List<Trade_History>> Get_OrderList_API(DateTime targetDate)
        {

            // 시작 시간: 00:00:00
            string targetDate_ISO_start = targetDate.Date.ToString("yyyy-MM-ddTHH:mm:sszzz");

            // 종료 시간: 23:59:59
            string targetDate_ISO_end = targetDate.Date.AddHours(23).AddMinutes(59).AddSeconds(59)
                                            .ToString("yyyy-MM-ddTHH:mm:sszzz");

            // 쿼리 스트링 생성
            var queryParams = new Dictionary<string, string>
            {
                { "limit", "1000" },
                 { "start_time", targetDate_ISO_start },
                {"end_time",  targetDate_ISO_end}
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

                
                // DateTime으로 변경
                orderLists.ForEach(dt => dt.CreatedAt_DT = DateTime.Parse(dt.CreatedAt));


                // Excuted Volume이 0인거 전부 제거
                orderLists.RemoveAll(EV => EV.ExecutedVolume == "0");


                // Trade History 형식으로 변환
                List<Trade_History> tradeHistory = new List<Trade_History>();


                // 내용 옮기기
                foreach(var order in orderLists)
                {
                    Trade_History tempHistory = new Trade_History();
                    
                    tempHistory.CoinName = order.Market.Split('-').Last();

                    // 없어진 코인일 수도 있음
                    if(CoinListData.Exists(a => a.CoinName == tempHistory.CoinName))
                    {
                        tempHistory.CoinNameKR = CoinListData.FirstOrDefault(a => a.CoinName == tempHistory.CoinName).CoinNameKR;
                        tempHistory.Side = order.Side == "bid" ? "매수" : "매도";
                        tempHistory.DateTime_CreatedAt = order.CreatedAt_DT;
                        tempHistory.Executed_Volume = decimal.Parse(order.ExecutedVolume);
                        tempHistory.Evaluated_Funds = tempHistory.Side == "매수" ? decimal.Parse(order.ExecutedFunds) + decimal.Parse(order.PaidFee) : decimal.Parse(order.ExecutedFunds) - decimal.Parse(order.PaidFee);

                        tradeHistory.Add(tempHistory);
                    }
                    else
                    {
                        Console.WriteLine("없어진 코인 " + tempHistory.CoinName);
                    }
                        
                }





                return tradeHistory;

            }
            else
            {
                Console.WriteLine($"API 요청 실패: {response.StatusCode} - {response.Content}");
                return null;
            }
        }


        // 주문 리스트 조회
        private async Task<List<Trade_History>> Get_OrderList_Today_API()
        {

            // 시작 시간: 00:00:00 어제부터
            string targetDate_ISO_start = DateTime.Now.Date.ToString("yyyy-MM-ddTHH:mm:sszzz");


            // 쿼리 스트링 생성
            var queryParams = new Dictionary<string, string>
            {
                { "limit", "1000" },
                 { "start_time", targetDate_ISO_start }
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


                // DateTime으로 변경
                orderLists.ForEach(dt => dt.CreatedAt_DT = DateTime.Parse(dt.CreatedAt));


                // Excuted Volume이 0인거 전부 제거
                orderLists.RemoveAll(EV => EV.ExecutedVolume == "0");


                // Trade History 형식으로 변환
                List<Trade_History> tradeHistory = new List<Trade_History>();


                // 내용 옮기기
                foreach (var order in orderLists)
                {
                    Trade_History tempHistory = new Trade_History();

                    tempHistory.CoinName = order.Market.Split('-').Last();

                    // 없어진 코인일 수도 있음
                    if (CoinListData.Exists(a => a.CoinName == tempHistory.CoinName))
                    {
                        tempHistory.CoinNameKR = CoinListData.FirstOrDefault(a => a.CoinName == tempHistory.CoinName).CoinNameKR;
                        tempHistory.Side = order.Side == "bid" ? "매수" : "매도";
                        tempHistory.DateTime_CreatedAt = order.CreatedAt_DT;
                        tempHistory.Executed_Volume = decimal.Parse(order.ExecutedVolume);
                        tempHistory.Evaluated_Funds = tempHistory.Side == "매수" ? decimal.Parse(order.ExecutedFunds) + decimal.Parse(order.PaidFee) : decimal.Parse(order.ExecutedFunds) - decimal.Parse(order.PaidFee);

                        tradeHistory.Add(tempHistory);
                    }
                    else
                    {
                        Console.WriteLine("없어진 코인 " + tempHistory.CoinName);
                    }

                }





                return tradeHistory;

            }
            else
            {
                Console.WriteLine($"API 요청 실패: {response.StatusCode} - {response.Content}");
                return null;
            }
        }

        // 호가 조회
        public async Task<List<Orderbook>> Get_Orderbook_API(string CoinName, double CurrentPrice, double CurrentChangeRate)
        {
            string apiUrl = $"https://api.upbit.com/v1/orderbook?markets={"KRW-" + CoinName}";

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
                        tempAsk.ChangeRate = (((price.AskPrice - CurrentPrice) / CurrentPrice) * 100) + CurrentChangeRate;

                        Orderbook tempBid = new Orderbook();
                        tempBid.Price = price.BidPrice;
                        tempBid.Volume = price.BidSize;
                        tempBid.ChangeRate = (((price.BidPrice - CurrentPrice) / CurrentPrice) * 100) + CurrentChangeRate;

                        tempOrderbooks.Add(tempAsk);
                        tempOrderbooks.Add(tempBid);
                    }

                    // 가격으로 재정렬
                    tempOrderbooks = tempOrderbooks.OrderBy(x => x.Price).ToList();


                    // 퍼센트 입력
                    double totalVolume = orderBooks[0].TotalBidSize + orderBooks[0].TotalAskSize;
                    foreach (var target in tempOrderbooks)
                    {
                        target.Percent = (target.Volume / totalVolume) * 100;
                    }

                    



                    return tempOrderbooks.ToList();
                }
                else
                {
                    Console.WriteLine($"API 요청 실패: {response.StatusCode}");
                    return null;
                }
            }
        }


        // 미체결 내역 조회
        public async Task<List<PendingOrder>> Get_PendingOrders_API(List<Account> accounts)
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
                var orders = JsonConvert.DeserializeObject<List<Upbit_PendingOrder>>(response.Content);

                // Market 속성에서 "KRW-" 제거
                foreach (var order in orders)
                {
                    if (order.Market.StartsWith("KRW-"))
                    {
                        order.Market = order.Market.Replace("KRW-", string.Empty);
                    }
                }

                // 미체결 정보
                List<PendingOrder> tempOrders = new List<PendingOrder>();

                foreach(var order in orders)
                {
                    PendingOrder temp = new PendingOrder();
                    var account = accounts.FirstOrDefault(a => a.CoinName == order.Market);

                    // 매수
                    if (order.Side == "bid")
                    {
                        // 내용 저장
                        temp.CoinName = order.Market;
                        temp.CoinNameKR = CoinListData.FirstOrDefault(c => c.CoinName == order.Market).CoinNameKR;
                        temp.Buying_Reserved_Price = double.Parse(order.Price);
                        temp.OrderType = "매수예약";
                        temp.Uuid = order.Uuid;
                    }
                    // 매도
                    else if(order.Side == "ask")
                    {
                        double orderPrice = double.Parse(order.Price);            // 주문가격
                        double avgBuyPrice = double.Parse(account.AvgBuyPrice);   // 평균 매수가

                        double priceDifferencePercent = ((orderPrice - avgBuyPrice) / avgBuyPrice) * 100;  // 평균 매수가 대비 차이 계산



                        // 내용 저장
                        temp.CoinName = order.Market;
                        temp.CoinNameKR = CoinListData.FirstOrDefault(c => c.CoinName == order.Market).CoinNameKR;
                        temp.Selling_Reserved_Rate = (double)priceDifferencePercent;
                        temp.OrderType = "매도예약";
                        temp.Uuid = order.Uuid;
                        temp.IsSellingPart = account.Volume != "0" ? true : false;
                    }



                    tempOrders.Add(temp);
                }


                return tempOrders.ToList();

            }
            else
            {
                Console.WriteLine($"API 요청 실패: {response.StatusCode} - {response.Content}");
                return null;
                
            }
        }


        // 미체결 거래 취소
        public async Task<bool> Cancel_PendingOrder_API(string UUID)
        {
            string queryString = $"uuid={UUID}";


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
            request.AddQueryParameter("uuid", UUID);

            var response = await client.ExecuteAsync(request);


            if (response.IsSuccessful)
            {
                // JSON 데이터를 객체로 변환
                var orders = JsonConvert.DeserializeObject<PendingOrder>(response.Content);

                return true;
            }
            else
            {
                Console.WriteLine($"API 요청 실패: {response.StatusCode} - {response.Content}");

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

        public class Upbit_Market
        {
            [JsonProperty("market")]
            public string Name_Market { get; set; }

            [JsonProperty("korean_name")]
            public string Name_KR { get; set; }

            [JsonProperty("english_name")]
            public string Name_ENG { get; set; }

            [JsonProperty("market_event")]
            public MarketEvent Market_Event { get; set; }
        }

        public class MarketEvent
        {
            [JsonProperty("warning")]
            public bool Warning { get; set; }

            [JsonProperty("caution")]
            public MarketCaution Caution { get; set; }
        }

        public class MarketCaution
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


        // Ticker 조회
        private class Upbit_Ticker
        {
            [JsonProperty("market")]
            public string Market { get; set; } = string.Empty; // 종목 구분 코드

            [JsonProperty("trade_date")]
            public string TradeDateUTC { get; set; } = string.Empty; // 최근 거래 일자 (UTC)

            [JsonProperty("trade_time")]
            public string TradeTimeUTC { get; set; } = string.Empty;// 최근 거래 시간 (UTC)

            [JsonProperty("trade_date_kst")]
            public string TradeDateKST { get; set; } = string.Empty; // 최근 거래 일자 (KST)

            [JsonProperty("trade_time_kst")]
            public string TradeTimeKST { get; set; } = string.Empty; // 최근 거래 시간 (KST)

            [JsonProperty("trade_timestamp")]
            public long TradeTimestamp { get; set; } = 0; // 최근 거래 일시 (UTC, Unix Timestamp)

            [JsonProperty("opening_price")]
            public double OpeningPrice { get; set; } = 0.0; // 시가

            [JsonProperty("high_price")]
            public double HighPrice { get; set; } = 0.0; // 고가

            [JsonProperty("low_price")]
            public double LowPrice { get; set; } = 0.0; // 저가

            [JsonProperty("trade_price")]
            public double TradePrice { get; set; } = 0.0; // 현재가

            [JsonProperty("prev_closing_price")]
            public double PrevClosingPrice { get; set; } = 0.0; // 전일 종가

            [JsonProperty("change")]
            public string Change { get; set; } = string.Empty; // 변동 상태 (EVEN, RISE, FALL)

            [JsonProperty("change_price")]
            public double ChangePrice { get; set; } = 0.0;// 변동액의 절대값

            [JsonProperty("change_rate")]
            public double ChangeRate { get; set; } = 0.0;// 변동률의 절대값

            [JsonProperty("signed_change_price")]
            public double SignedChangePrice { get; set; } = 0.0;// 부호가 있는 변동액

            [JsonProperty("signed_change_rate")]
            public double SignedChangeRate { get; set; } = 0.0;// 부호가 있는 변동률

            [JsonProperty("trade_volume")]
            public double TradeVolume { get; set; } = 0.0;// 가장 최근 거래량

            [JsonProperty("acc_trade_price")]
            public double AccTradePrice { get; set; } = 0.0;// 누적 거래대금 (UTC 0시 기준)

            [JsonProperty("acc_trade_price_24h")]
            public double AccTradePrice24h { get; set; } = 0.0; // 24시간 누적 거래대금

            [JsonProperty("acc_trade_volume")]
            public double AccTradeVolume { get; set; } = 0.0;// 누적 거래량 (UTC 0시 기준)

            [JsonProperty("acc_trade_volume_24h")]
            public double AccTradeVolume24h { get; set; } = 0.0; // 24시간 누적 거래량

            [JsonProperty("highest_52_week_price")]
            public double Highest52WeekPrice { get; set; } = 0.0;// 52주 신고가

            [JsonProperty("highest_52_week_date")]
            public string Highest52WeekDate { get; set; } = string.Empty;// 52주 신고가 달성일

            [JsonProperty("lowest_52_week_price")]
            public double Lowest52WeekPrice { get; set; } = 0.0;// 52주 신저가

            [JsonProperty("lowest_52_week_date")]
            public string Lowest52WeekDate { get; set; } = string.Empty; // 52주 신저가 달성일

            [JsonProperty("timestamp")]
            public long Timestamp { get; set; } = 0; // 타임스탬프
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

            [JsonProperty("executed_funds")]
            public string ExecutedFunds { get; set; } // 체결된 금액 (NumberString)


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
            public string Message { get; }

            public OrderEventArgs(string message)
            {
                Message = message;
            }
        }


        // 탐색 업데이트 이벤트
        public event EventHandler<SearchEventArgs> SearchRetrieved;
        public class SearchEventArgs : EventArgs
        {
            public Search_Data Search_Data { get; set; }
            public string Message { get; }

            public SearchEventArgs(Search_Data SearchData, string message)
            {
                if(SearchData != null)
                {
                    Search_Data = (Search_Data)SearchData.Clone();
                }
                else
                {
                    Search_Data = null;
                }
                Message = message;
            }
        }



        #endregion





        #region 코인 탐색


        public class Search_Data
        {
            public Search_Data()
            {
                Candles_5 = new List<CandleData>();
                Candles_10 = new List<CandleData>();
                Candles_30 = new List<CandleData>();
                Candles_60 = new List<CandleData>();
                Candles_240 = new List<CandleData>();
                Candles_Days = new List<CandleData>();
            }

            public List<CandleData> Candles_Days;
            public List<CandleData> Candles_240;
            public List<CandleData> Candles_60;
            public List<CandleData> Candles_30;
            public List<CandleData> Candles_10;
            public List<CandleData> Candles_5;


            public object Clone()
            {
                return new Search_Data
                {
                    Candles_5 = Candles_5.ToList(),
                    Candles_10 = Candles_10.ToList(),
                    Candles_30 = Candles_30.ToList(),
                    Candles_60 = Candles_60.ToList(),
                    Candles_240 = Candles_240.ToList(),
                    Candles_Days = Candles_Days.ToList()
                };
            }
        }


        // 분봉 전략 1 지우기
        public void CoinSearch_Strat1_Clear_Container()
        {
            _Search_Data.Candles_Days.Clear();
            _Search_IsRunning_Days = true;

            _Search_Data.Candles_60.Clear();
            _Search_IsRunning_60 = true;
        }



        // 60분봉 부터 체우기 시작. 매 정각마다 다시 체우기
        // 60분봉이 다 차면 일봉 체우기 시작
        public Search_Data _Search_Data = new Search_Data();
        int _Search_Index_60 = 0, _Search_Index_30 = 0, _Search_Index_10 = 0, _Search_Index_5 = 0;
        bool _Search_IsRunning_Days = false, _Search_IsRunning_60 = false;
        bool _Search_Alarm = false;
        async void CoinSearch_Processing()
        {
            // 총 코인 수
            int n_of_coins = CoinListData.Count;


            DateTime tempDT = DateTime.Now;

            // 매 시 50분이면 초기화
            if ((tempDT.Minute == 50) && (tempDT.Second < 2))
            {
                _Search_Data.Candles_60.Clear();
                _Search_IsRunning_60 = true;

                _Search_Data.Candles_Days.Clear();
                _Search_IsRunning_Days = true;
            }

            
            // 60 분봉 체우기
            else if ((_Search_Data.Candles_60.Count < n_of_coins) && _Search_IsRunning_60)
            {
                CandleData tempCandleData = await Get_Candle_Minutes_API(CoinListData[_Search_Data.Candles_60.Count].CoinName, 60, 200);
                
                _Search_Data.Candles_60.Add(tempCandleData);

                // 60분봉 다 찼음
                if (_Search_Data.Candles_60.Count == n_of_coins)
                {
                    _Search_IsRunning_60 = false;

                    //SearchRetrieved?.Invoke(this, new SearchEventArgs(_Search_Data, "60분봉"));
                }
            }
            // 일봉 체우기
            else if ((_Search_Data.Candles_Days.Count < n_of_coins) && _Search_IsRunning_Days)
            {
                CandleData tempCandleData = await Get_Candle_Days_API(CoinListData[_Search_Data.Candles_Days.Count].CoinName, 200);


                _Search_Data.Candles_Days.Add(tempCandleData);

                // 일봉 다 찼음
                if (_Search_Data.Candles_Days.Count == n_of_coins)
                {
                    _Search_IsRunning_Days = false;

                    SearchRetrieved?.Invoke(this, new SearchEventArgs(_Search_Data, "캔들완료"));
                }
            }


            // 매 시 59분 58초~59초면 알람
            if ((tempDT.Minute == 59) && (tempDT.Second > 50) && (_Search_Alarm == false))
            {
                _Search_Alarm = true;

                SearchRetrieved?.Invoke(this, new SearchEventArgs(null, "알람"));
            }
            else if ((tempDT.Minute == 0) && (tempDT.Second < 2))
            {
                // alarm clear
                _Search_Alarm = false;
            }


        }





        #endregion




        #region 거래내역 관련



        List<Trade_History> allTradeHistory = new List<Trade_History>();
        List<Trade_History> TodayTradeHistory = new List<Trade_History>();

        public List<Trade_Profits_Days> TradeProfits_Days = new List<Trade_Profits_Days>();

        int _Recent_Total_KRW = 0;

        // 오늘 거래 내역 업데이트
        public async Task<TodayTradesResults> Update_Today_Trades(int Total_KRW)
        {
            // 오늘거래결과
            TodayTradesResults tempTradeResult = new TodayTradesResults();

            // 오늘 거래 내역 가져오기
            List<Trade_Profits> tempProfits = await Calc_Profits_Days_Today();

            if (tempProfits != null) 
            {
                tempTradeResult.Trade_Profits = tempProfits.ToList();

                // 수익 카운터
                tempTradeResult.Profit_cnt = tempTradeResult.Trade_Profits.Count(a => a.Profit > 0);

                // 손해 카운터
                tempTradeResult.Loss_cnt = tempTradeResult.Trade_Profits.Count(a => a.Profit < 0);

                // 총 수익금
                tempTradeResult.Profit_Sum = tempTradeResult.Trade_Profits.Where(a => a.Profit > 0).Sum(a => a.Profit);

                // 총 손해금
                tempTradeResult.Loss_Sum = tempTradeResult.Trade_Profits.Where(a => a.Profit < 0).Sum(a => a.Profit);

                // 총 이득
                tempTradeResult.ProfitLoss_Sum = tempTradeResult.Profit_Sum + tempTradeResult.Loss_Sum;


                // 알람 여부 확인 (총 자산이 바뀌었는가)
                tempTradeResult.IsProfitChanged = false;
                tempTradeResult.ChangedProfit = 0;
                if ((_Recent_Total_KRW != Total_KRW) && (_Recent_Total_KRW != 0))
                {
                    // 차이가 100원 이상 날 경우만
                    int gap = Total_KRW - _Recent_Total_KRW;

                    if((gap > 100) || (gap < -100))
                    {
                        tempTradeResult.IsProfitChanged = true;
                        tempTradeResult.ChangedProfit = gap;
                    }
                    
                }

                // 총 자산 업데이트
                _Recent_Total_KRW = Total_KRW;
            }

            return tempTradeResult;
        }





      
        // 오늘 거래 일별 계산하기
        public async Task<List<Trade_Profits>> Calc_Profits_Days_Today()
        {
            List<Trade_Profits> temptradeProfits = new List<Trade_Profits>();


            // 오늘 거래내역 가져오기
            List<Trade_History> results = await Get_OrderList_Today_API();
            

            // 매도만 가져오기
            List<Trade_History> sell_results = results.ToList();
            sell_results.OrderByDescending(t => t.DateTime_CreatedAt).ToList();
            sell_results.RemoveAll(a => a.Side == "매수");

            // 연속된 매도 합치기 (시간이 3초 안에 있는거)
            sell_results = Combine_ContinuedTrades(sell_results);

            // 매도 거래가 없으면 null 반환
            if(sell_results.Count == 0)
            {
                return null;
            }


            // 해당 매도 거래에 대한 매수 금액 찾아내기
            foreach (Trade_History t in sell_results)
            {
                // 오늘 거래 내역에서 매수 찾기
                decimal bought_price = Find_Sell_Price_from_Today_History(t.CoinNameKR, t.Executed_Volume, t.DateTime_CreatedAt, results.ToList());

                // 과거 거래 내역에서 매수 찾기
                if(bought_price == 0)
                {
                    bought_price = Find_Sell_Price_from_Prev_History(t.CoinNameKR, t.Executed_Volume, t.DateTime_CreatedAt);
                }

                // 내용이 있으면
                if (bought_price != 0)
                {
                    // 거래 손익 만들기
                    Trade_Profits tempProfit = new Trade_Profits();

                    tempProfit.CoinName = t.CoinName;
                    tempProfit.CoinNameKR = t.CoinNameKR;
                    tempProfit.Profit = t.Evaluated_Funds - bought_price;

                    temptradeProfits.Add(tempProfit);
                }
            }


            return temptradeProfits;
        }

        

        // 과거 일별 계산하기
        public void Calc_Profits_Days_Previous()
        {

            // 1. 전체 매도 거래 내역을 최신순 정렬
            List<Trade_History> sortedHistory = allTradeHistory.OrderByDescending(t => t.DateTime_CreatedAt).ToList();
            sortedHistory.RemoveAll(a => a.Side == "매수");

            // 연속된 매도 거래 합치기
            sortedHistory = Combine_ContinuedTrades(sortedHistory);


            // 해당 일 데이터 저장용 버퍼
            Trade_Profits_Days tradeDays_temp = new Trade_Profits_Days();
            tradeDays_temp.Trade_Profits = new List<Trade_Profits>();
            tradeDays_temp.Date = sortedHistory.First().DateTime_CreatedAt.Date;
            

            // 해당 매도 거래에 대한 매수 금액 찾아내기
            foreach (Trade_History t in sortedHistory)
            {
                decimal bought_price = Find_Sell_Price_from_Prev_History(t.CoinNameKR, t.Executed_Volume, t.DateTime_CreatedAt);

                if(bought_price != 0)
                {
                    // 거래 손익 만들기
                    Trade_Profits tempProfit = new Trade_Profits();



                    // 날짜가 같으면 내용 추가
                    if (tradeDays_temp.Date == t.DateTime_CreatedAt.Date)
                    {
                        tempProfit.CoinName = t.CoinName;
                        tempProfit.CoinNameKR = t.CoinNameKR;
                        tempProfit.Profit = t.Evaluated_Funds - bought_price;

                        tradeDays_temp.Trade_Profits.Add(tempProfit);
                    }
                    // 날짜가 다르면 쌓인 데이터 저장하고 버퍼 내용 변경하기
                    else
                    {
                        // 리스트에 저장
                        TradeProfits_Days.Add(tradeDays_temp);

                        // 버퍼 내용 변경
                        tradeDays_temp = new Trade_Profits_Days();
                        tradeDays_temp.Trade_Profits = new List<Trade_Profits>();
                        tradeDays_temp.Date = t.DateTime_CreatedAt.Date;

                        // 내용 추가
                        tempProfit.CoinName = t.CoinName;
                        tempProfit.CoinNameKR = t.CoinNameKR;
                        tempProfit.Profit = t.Evaluated_Funds - bought_price;

                        tradeDays_temp.Trade_Profits.Add(tempProfit);
                    }
                }
                
            }
        }



        // 오늘 데이터에서 해당 코인의 볼륨에 맞는 매수금액 가져오기
        private decimal Find_Sell_Price_from_Today_History(string CoinNameKR, decimal Volume, DateTime DT_Sold, List<Trade_History> Trade_History_Today)
        {
            // 오늘 거래 내역 가져오기
            List<Trade_History> sortedHistory = Trade_History_Today.ToList();

            // 금일 거래 내역 추가하기
            sortedHistory.AddRange(TodayTradeHistory.ToList());

            // 전체 매수 거래 내역을 최신순 정렬
            allTradeHistory.OrderByDescending(t => t.DateTime_CreatedAt).ToList();

            // 타겟 코인 정보만 남기고 제거
            sortedHistory.RemoveAll(a => a.CoinNameKR != CoinNameKR);
            sortedHistory.RemoveAll(a => a.Side == "매도");
            sortedHistory.RemoveAll(a => a.DateTime_CreatedAt > DT_Sold);


            // 남은 볼륨
            decimal remaining_volume = Volume;


            // 구매액
            decimal bought_funds = 0m;


            // 볼륨 제거하면서 구매액 더해넣기
            for (int i = 0; i < sortedHistory.Count; i++)
            {
               
                
                // 매수 볼륨이 더 크다.
                if(sortedHistory[i].Executed_Volume > remaining_volume)
                {
                    // 구매액을 다 넣지 말고 일부만 넣어라
                    decimal unit_price = sortedHistory[i].Evaluated_Funds / sortedHistory[i].Executed_Volume;   // 볼륨당 단가 계산
                    bought_funds += unit_price * remaining_volume;          // 남은 볼륨 만큼만 매수액 다시 계산

                    remaining_volume = 0;   // 볼륨 제거
                }
                else
                {
                    bought_funds += sortedHistory[i].Evaluated_Funds;   // 구매액 넣기

                    remaining_volume -= sortedHistory[i].Executed_Volume;   // 볼륨 제거
                }
                


                // 남은 볼륨 끝났음. (0.001% 보다 작게 남음)
                if (remaining_volume < Volume * 0.00001m)
                {
                    break;
                }
                // 볼륨 에러
                else if (remaining_volume < 0)
                {
                    return 0m;
                }
            }


            return bought_funds;

        }



        // 과거 데이터에서 해당 코인의 볼륨에 맞는 매수금액 가져오기
        private decimal Find_Sell_Price_from_Prev_History(string CoinNameKR, decimal Volume, DateTime DT_Sold)
        {
            // 과거 거래 내역 가져오기
            List<Trade_History> sortedHistory = allTradeHistory.ToList();

            // 금일 거래 내역 추가하기
            sortedHistory.AddRange(TodayTradeHistory.ToList());

            // 전체 매수 거래 내역을 최신순 정렬
             allTradeHistory.OrderByDescending(t => t.DateTime_CreatedAt).ToList();

            // 타겟 코인 정보만 남기고 제거
            sortedHistory.RemoveAll(a => a.CoinNameKR != CoinNameKR);
            sortedHistory.RemoveAll(a => a.Side == "매도");
            sortedHistory.RemoveAll(a => a.DateTime_CreatedAt >  DT_Sold);
           

            // 남은 볼륨
            decimal remaining_volume = Volume;


            // 구매액
            decimal bought_funds = 0m;


            // 볼륨 제거하면서 구매액 더해넣기
            for(int i=0; i< sortedHistory.Count; i++)
            {
                // 매수 볼륨이 더 크다.
                if (sortedHistory[i].Executed_Volume > remaining_volume)
                {
                    // 구매액을 다 넣지 말고 일부만 넣어라
                    decimal unit_price = sortedHistory[i].Evaluated_Funds / sortedHistory[i].Executed_Volume;   // 볼륨당 단가 계산
                    bought_funds += unit_price * remaining_volume;          // 남은 볼륨 만큼만 매수액 다시 계산

                    remaining_volume = 0;   // 볼륨 제거
                }
                else
                {
                    bought_funds += sortedHistory[i].Evaluated_Funds;   // 구매액 넣기

                    remaining_volume -= sortedHistory[i].Executed_Volume;   // 볼륨 제거
                }


                // 남은 볼륨 끝났음. (0.001% 보다 작게 남음)
                if (remaining_volume < Volume * 0.00001m)
                {
                    break;
                }
                // 볼륨 에러
                else if(remaining_volume < 0)
                {
                    return 0m;
                }
            }


            return bought_funds;

        }



        // 연속된 코인 이름 거래 합치기
        private List<Trade_History> Combine_ContinuedTrades(List<Trade_History> TradeHistory)
        {
            List<Trade_History> new_History = new List<Trade_History>();


            // 거래 내역이 1개면 그냥 넘겨
            if (TradeHistory.Count == 1)
            {
                new_History.Add(TradeHistory[0]);
            }
            else
            {
                for (int i = 0; i < TradeHistory.Count; i++)
                {
                    // 맨 마지막꺼 전까지만
                    if (i <= TradeHistory.Count - 2)
                    {
                        // 다음 코인이랑 이름이 같은지
                        if (TradeHistory[i].CoinNameKR == TradeHistory[i + 1].CoinNameKR)
                        {
                            // 시간차이가 3초 이내인지
                            if (Math.Abs((TradeHistory[i].DateTime_CreatedAt - TradeHistory[i + 1].DateTime_CreatedAt).TotalSeconds) <= 3)
                            {
                                Trade_History tempHistory = new Trade_History();
                                tempHistory.CoinName = TradeHistory[i].CoinName;
                                tempHistory.CoinNameKR = TradeHistory[i].CoinNameKR;
                                tempHistory.Side = TradeHistory[i].Side;
                                tempHistory.DateTime_CreatedAt = TradeHistory[i].DateTime_CreatedAt;    // 날짜는 최근걸로
                                tempHistory.Evaluated_Funds = TradeHistory[i].Evaluated_Funds + TradeHistory[i + 1].Evaluated_Funds;
                                tempHistory.Executed_Volume = TradeHistory[i].Executed_Volume + TradeHistory[i + 1].Executed_Volume;

                                // 하나 건너띄기
                                i++;
                            }
                            // 3초 이상 차이남. 다른 건수로 인식
                            else
                            {
                                new_History.Add(TradeHistory[i]);

                                // 마지막꺼 저장
                                if (i == TradeHistory.Count - 2)
                                {
                                    new_History.Add(TradeHistory[i + 1]);
                                }
                            }
                            
                        }
                        // 이름 다름
                        else
                        {
                            new_History.Add(TradeHistory[i]);

                            // 마지막꺼 저장
                            if(i == TradeHistory.Count - 2)
                            {
                                new_History.Add(TradeHistory[i + 1]);
                            }
                        }
                    }


                }
            }

            return new_History;
        }



        #endregion





        #region 거래내역 파일 저장 및 불러오기


        // 선택된 날 파일로 저장하기
        public async void TradeHistory_Save_as_File_SelectedDay(DateTime TargetDateTime)
        {

            // 기존 함수 호출, 날짜만 변경
            List<Trade_History> results = await Get_OrderList_API(TargetDateTime.Date);

            if (results != null && results.Count > 0)
            {
                SaveTradeHistory(results, _tradeHistoryPath);
            }
        }

        // 어제꺼 파일로 저장하기
        public async void TradeHistory_Save_as_File_YesterDay()
        {
            // 기존 함수 호출, 날짜만 변경
            List<Trade_History> results = await Get_OrderList_API(DateTime.Now.AddDays(-1).Date);

            if (results != null && results.Count > 0)
            {
                SaveTradeHistory(results, _tradeHistoryPath);
            }
        }




        // 거래내역 파일로 저장하기 - 기간
        public async void TradeHistory_Save_as_File_Period()
        {
            DateTime startDate = new DateTime(2025, 1, 1);
            DateTime endDate = new DateTime(2025, 5, 11);

            for (DateTime currentDate = startDate; currentDate <= endDate; currentDate = currentDate.AddDays(1))
            {
                Console.WriteLine($"📅 {currentDate:yyyy-MM-dd} 거래 내역 저장 중...");

                try
                {
                    // 기존 함수 호출, 날짜만 변경
                    List<Trade_History> results = await Get_OrderList_API(currentDate);

                    if (results != null && results.Count > 0)
                    {
                        SaveTradeHistory(results, _tradeHistoryPath);
                        Console.WriteLine($"✅ {currentDate:yyyy-MM-dd} 저장 완료!");
                    }
                    else
                    {
                        Console.WriteLine($"⚠️ {currentDate:yyyy-MM-dd} 거래 내역 없음.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ {currentDate:yyyy-MM-dd} 저장 중 오류 발생: {ex.Message}");
                }

                // 200ms 딜레이 추가 (API 과부하 방지)
                await Task.Delay(200);
            }

            Console.WriteLine("🎉 모든 거래 내역 저장 완료!");
        }

        // 주문결과 파일로 저장하는 함수
        public static void SaveTradeHistory(List<Trade_History> tradeHistory, string FilePath)
        {
            if (tradeHistory == null || tradeHistory.Count == 0)
            {
                //Console.WriteLine("❌ 저장할 데이터가 없습니다.");
                return;
            }

            // 첫 번째 아이템의 DateTime_CreatedAt.Date 기준으로 파일명 생성
            string fileName = tradeHistory[0].DateTime_CreatedAt.Date.ToString("yyyy-MM-dd") + ".json";
            string filePath = Path.Combine(FilePath, fileName);

            try
            {
                // 폴더가 없으면 생성
                Directory.CreateDirectory(FilePath);

                // JSON 직렬화 후 저장
                string json = JsonConvert.SerializeObject(tradeHistory, Formatting.Indented);
                File.WriteAllText(filePath, json);

                Console.WriteLine($"✅ 거래 내역 저장 완료! → {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 파일 저장 중 오류 발생: {ex.Message}");
            }
        }


        // 주문결과 파일들을 전체 불러오는 함수
        private void LoadAllTradeHistory(string FilePath)
        {
            try
            {
                // 폴더가 없으면 생성 (비어있는 경우라도 에러 방지)
                Directory.CreateDirectory(FilePath);

                // 해당 경로에 있는 모든 `.json` 파일 가져오기
                string[] files = Directory.GetFiles(FilePath, "*.json");

                if (files.Length == 0)
                {
                    Console.WriteLine("❌ 불러올 거래 내역 파일이 없습니다.");
                }

                // 해당 거래 기록 다 가져오기
                foreach (string file in files)
                {
                    try
                    {
                        // 오늘꺼 제외 모두 읽어오기
                        if (file != DateTime.Now.Date.ToString("yyyy-MM-dd") + ".json")
                        {
                            // JSON 파일 읽고 역직렬화
                            string json = File.ReadAllText(file);
                            List<Trade_History> tradeHistory = JsonConvert.DeserializeObject<List<Trade_History>>(json);

                            allTradeHistory.AddRange(tradeHistory);
                        }


                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ 파일 {file} 읽기 오류: {ex.Message}");
                    }
                }

                // 📌 최신 날짜 기준으로 정렬 (내림차순)
                allTradeHistory = allTradeHistory.OrderByDescending(trade => trade.DateTime_CreatedAt).ToList();



                // 과거 일별 수익 계산하기
                Calc_Profits_Days_Previous();




                Console.WriteLine($"✅ 총 {allTradeHistory.Count}개의 거래 내역 불러오기 완료!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 거래 내역 불러오기 중 오류 발생: {ex.Message}");
            }

        }


        #endregion


        #region Functions


        // 볼륨 자리수 맞추면서 조정
        double AdjustVolume(double volume, double ratio)
        {
            // 내부에서 소수점 자리수 구함 (수정된 방식)
            string strValue = volume.ToString("0.################", System.Globalization.CultureInfo.InvariantCulture);
            int dotIndex = strValue.IndexOf('.');
            int decimalPlaces = (dotIndex == -1) ? 0 : (strValue.Length - dotIndex - 1);

            // volume * ratio 한 다음, 소수점 자리수 맞춰서 버림
            double adjusted = volume * ratio;
            double factor = Math.Pow(10, decimalPlaces);
            return Math.Truncate(adjusted * factor) / factor;
        }



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


        // 지정가 소수점 맞추기 (new)
        public decimal GetAdjustedLimitPrice(decimal price)
        {
            decimal unit;

            if (price >= 2_000_000m)
                unit = 1000m;
            else if (price >= 1_000_000m)
                unit = 1000m;
            else if (price >= 500_000m)
                unit = 500m;
            else if (price >= 100_000m)
                unit = 100m;
            else if (price >= 50_000m)
                unit = 50m;
            else if (price >= 10_000m)
                unit = 10m;
            else if (price >= 5_000m)
                unit = 5m;
            else if (price >= 1_000m)
                unit = 1m;
            else if (price >= 100m)
                unit = 1m;
            else if (price >= 10m)
                unit = 0.1m;
            else if (price >= 1m)
                unit = 0.01m;
            else if (price >= 0.1m)
                unit = 0.001m;
            else if (price >= 0.01m)
                unit = 0.0001m;
            else if (price >= 0.001m)
                unit = 0.00001m;
            else if (price >= 0.0001m)
                unit = 0.000001m;
            else if (price >= 0.00001m)
                unit = 0.0000001m;
            else
                unit = 0.00000001m;

            return Math.Floor(price / unit) * unit;
        }



        // 지정가 소수점 맞추기 (old)
        public decimal GetAdjustedLimitPriceOld(string CoinName, decimal Sell_Price)
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



        // 전체 매수 금액 갖고오기
        decimal Calc_Total_Buyed_Price(List<Account> accounts)
        {
            if(accounts != null)
            {
                List<Account> tempAccount = accounts.ToList();

                decimal tempTotal = 0;

                tempAccount.RemoveAll(a => a.CoinName == "KRW");
                foreach (var account in tempAccount)
                {
                    decimal avgPrice = decimal.Parse(account.AvgBuyPrice);
                    decimal volume = account.Volume == "0" ? decimal.Parse(account.Volume_Pending) : decimal.Parse(account.Volume);

                    tempTotal += volume * avgPrice;
                }

                return tempTotal;
            }
            return 0;
        }


        // 전체 평가 금액 갖고오기
        decimal Calc_Total_Evaluated_Price(List<Account> accounts, List<Ticker> tickers)
        {
            List<Account> tempAccount = accounts.ToList();

            decimal tempTotal = 0;

            tempAccount.RemoveAll(a => a.CoinName == "KRW");
            foreach (var account in tempAccount)
            {
                decimal currentPrice = (decimal)tickers.FirstOrDefault(a => a.CoinName == account.CoinName).Trade_Price;
                decimal volume = account.Volume == "0" ? decimal.Parse(account.Volume_Pending) : decimal.Parse(account.Volume);

                tempTotal += volume * currentPrice;
            }

            return tempTotal;
        }


        #endregion




    }
}
