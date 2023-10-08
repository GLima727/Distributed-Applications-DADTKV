using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DADTKV.transactionManager
{
    class TransactionManager
    {
        private string _id = "";
        public string Id {
            get {
                return _id;
            }
            set {
                _id = value;
            }
        }

        public TransactionManager() {

        }
    }
}
