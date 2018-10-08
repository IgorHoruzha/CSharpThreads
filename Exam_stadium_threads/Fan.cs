using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exam_stadium_threads
{
    public class Fan
    {
        public bool HasTicket { get;  set; }
        public ushort SectorNumber { get; set; }
        public ushort PlaceNumber { get; set; }
        public string Name { get; set; }

        public string TicketInfo { get { return this.ToString(); } }

        public override string ToString()
        {
            return $"Name: {Name} Valid Ticket:{HasTicket}  SectorNumber: {SectorNumber} PlaceNumber:{PlaceNumber}";
        }
    }
}
