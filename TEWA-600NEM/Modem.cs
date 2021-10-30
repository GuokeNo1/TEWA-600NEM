using LibModemBase;
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace TEWA_600NEM
{
    public class Modem:ModemBase
    {
        public static new string modem = "TEWA-600NEM";
        public static new bool needUser = false;
        public static new bool needLogin = true;
        public Modem(string host, string passwd) : base(host, passwd) { }
        private bool login()
        {
            string url = $"http://{host}/login.cgi";
            string body = EasyWeb.Post(url, $"password={passwd}").body;
            return !body.Contains("parent.location='login_smart.html'");
        }
        private void logout()
        {
            string url = $"http://{host}/logout.cgi";
            EasyWeb.Get(url);
        }
        public override string getModemInfo()
        {
            if (login())
            {
                string url = $"http://{host}/dumpcfg.conf";

                string responseStr = EasyWeb.Get(url).body;
                Regex regex = new Regex(@"<body[^>]*>([\s\S]*)<\/body>");
                Match m = regex.Match(responseStr);
                string outText = m.Groups[1].Value.Trim();
                outText = outText.Replace("&lt;", "<").Replace("&nbsp;", " ").Replace("&gt;", ">").Replace("<br>", "\n").Replace("&amp;", "&").Replace("&#32;", " ");
                outText = outText.Substring(0, outText.IndexOf("Dump bytes allocated=")).Trim();

                regex = new Regex(@"<X_CT-COM_TeleComAccount>([\s\S]*)</X_CT-COM_TeleComAccount>");
                m = regex.Match(outText);
                string superPasswd = $"超级管理员密码:{m.Groups[1].Value.Replace("<Password>", "").Replace("</Password>", "").Trim()}";

                regex = new Regex(@"<WANPPPConnection[^>]*>([\s\S]*)</WANPPPConnection>");
                MatchCollection mc = regex.Matches(outText);
                string pppoe = "";
                foreach (Match mm in mc)
                {
                    regex = new Regex(@"<Username>([\s\S]*)</Username>");
                    m = regex.Match(mm.Value);
                    pppoe += $"宽带账号:{m.Groups[1].Value}\n";
                    regex = new Regex(@"<Password>([\s\S]*)</Password>");
                    m = regex.Match(mm.Value);
                    byte[] bs = Convert.FromBase64String(m.Groups[1].Value);
                    pppoe += $"宽带密码:{Encoding.UTF8.GetString(bs)}\n";
                }
                logout();
                return $"型号:{modem}\n\n{superPasswd}\n\n{pppoe}";
            }
            else
            {
                return "获取失败\r\n登录失败，密码错误！";
            }
        }
    }
}
