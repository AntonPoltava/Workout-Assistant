using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramUI.Models
{
    public class ExerciseDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string MediaUrl { get; set; }
        public int Category { get; set; }
    }
    public class UserExerciseSession
    {
        public List<ExerciseDto> Exercises { get; set; } = new();
        public int CurrentIndex { get; set; } = 0;
        public string Mode { get; set; }
    }




}
