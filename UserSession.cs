using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II
{
    // O clasă statică e vizibilă de oriunde din aplicație, ca o memorie globală
    public static class UserSession
    {
        public static string CurrentUsername { get; set; } = "Vizitator Anonim";
    }
}
