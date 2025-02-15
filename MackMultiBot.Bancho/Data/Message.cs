using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Bancho.Data
{
	internal class Message
	{
        public string Channel { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public DateTime Created { get; set; } = DateTime.UtcNow;

        public DateTime Sent { get; set; }
    }
}
