using System.Collections.Generic;
using MyLib;

namespace MyLib
{
    public class DataBase
    {
        private readonly object token = new object();

        Dictionary<string, double> dic = new Dictionary<string, double>();

        public DataBase()
        {
            dic.Add("uno", 10000);
            dic.Add("dos", 10000);
            dic.Add("tres", 10000);
            dic.Add("cuatro", 10000);
            dic.Add("cinco", 10000);
            dic.Add("seis", 10000);
            dic.Add("siete", 10000);
            dic.Add("ocho", 10000);
            dic.Add("nueve", 10000);
            dic.Add("diez", 10000);
        }

        private void Deposit(string user, double amount)
        {
            lock (token)
            {
                dic[user] = dic[user] + amount;
            }
        }

        private void Withdraw(string user, double amount)
        {
            lock (token)
            {
                dic[user] = dic[user] - amount;
            }
        }

        public bool Transfer(string from, string to, double amount)
        {
            lock (token)
            {
                if ((from == null) || (to == null) || !dic.ContainsKey(from) || !dic.ContainsKey(to))
                {
                    return false;
                }
                else
                {
                    if (dic[from] < amount)
                    {
                        return false;
                    }
                    else
                    {
                        Withdraw(from, amount);
                        Deposit(to, amount);
                        return true;
                    }
                }
            }
        }
        public bool Transfer(Transaction op)
        {
            return Transfer(op.From, op.To, op.Amount);
        }
        public double Balance(string user)
        {
            lock (token)
            {
                return dic[user];
            }
        }

    }
}
