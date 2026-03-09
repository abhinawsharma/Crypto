using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace TMCryptoCore.Model
{
    public class ASPSessionStateModel
    {
        [Column("GUID")]
        public string GUID { get; set; }
        [Column("SessionKey")]
        public string SessionKey { get; set; }
        [Column("SessionValue")]
        public string SessionValue { get; set; }
        [Column("DateTime_Added")]
        public DateTime? DateTime_Added { get; set; }
       
    }
}
