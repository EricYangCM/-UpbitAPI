using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ThisisIt_v4._0.Upbit;
using static ThisisIt_v4._0.Upbit.UpbitApi;

namespace ThisisIt_v4._0.Login
{
    public partial class form_keyCheck : Form
    {
        public form_keyCheck()
        {
            InitializeComponent();

            Init_Key();

            bworker_KeyFinder.RunWorkerAsync();
        }

        public string KeyName = "";
        public string Access_Key = "";
        public string Secre_Key = "";


        List<string[]> _List_Keys = new List<string[]>();
        void Init_Key()
        {
            // Key 1 - 집
            _List_Keys.Add(new string[]
            {
                "집",                                        // Key Name
                "YeUi0uDOLWS3nRg7ZLQx0us8lOCAFJfsnskh5Ges",  // Access Key
                "sQxNdfYxRp4TOsvXtgH5bIFHypEiphASh1Mm91ac"      // Secret Key
            });

            // Key 2 - 회사
            _List_Keys.Add(new string[]
            {
                "회사",                                        // Key Name
                "KplodurLzqlDXFdMs9pSRR2gB6KX63mmH4L9XGbu",  // Access Key
                 "TQAlwVj9ycW9Byce23fOsFSxiOENiWiLYAZDibXa"      // Secret Key
            });

            // Key 3 - 스톰
            _List_Keys.Add(new string[]
            {
                "스톰",                                        // Key Name
                "x25ki8bOuxgPyizqxXx5QJXWpYtUq5kRVAHWxHCq",  // Access Key
                 "TbYKJMjgZtrWHXaARif5EJ96jVEgURyxHMr3AA74"      // Secret Key
            });

            // Key 4 - 태영빌라
            _List_Keys.Add(new string[]
            {
                "태영빌라",                                        // Key Name
                "lujhpTdRHaKpb1xDoA0EnknfOIoX5eXZj5TPxS6g",     // Access Key
                 "6JgRxGFWiPLa8cu6fJsAfuqrbA1qgWLI6xsxrdev"      // Secret Key
            });
        }


        async private void bworker_KeyFinder_DoWork(object sender, DoWorkEventArgs e)
        {
            Thread.Sleep(100);

            for(int i=0; i< _List_Keys.Count; i++)
            {
                this.Invoke(new MethodInvoker(delegate ()
                {
                    label_targetKey.Text = _List_Keys[i][0].ToString();
                }));

                Thread.Sleep(500);

                // Key
                UpbitApi _Upbit = new UpbitApi(_List_Keys[i][1], _List_Keys[i][2]);

                // Get Account List
                List<Account> tempAccountList = await _Upbit.GetAccountsAsync();

                // Check Account
                if (tempAccountList != null)
                {
                    KeyName = _List_Keys[i][0];
                    Access_Key = _List_Keys[i][1];
                    Secre_Key = _List_Keys[i][2];

                    break;
                }
            }


            if(Access_Key != "")
            {
                this.Invoke(new MethodInvoker(delegate ()
                {
                    // Close
                    this.Close();
                }));
            }
            else
            {
                this.Invoke(new MethodInvoker(delegate ()
                {
                    label_indiciator.Text = "맞는 키가 없습니다.";
                }));
            }

           
        }
    }
}
