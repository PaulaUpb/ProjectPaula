﻿using Newtonsoft.Json;
using ProjectPaula.DAL;
using System.Collections.Generic;
using System.Linq;

namespace ProjectPaula.Model
{
    [JsonObject(IsReference = true)]
    public class SelectedCourse
    {
        public int Id { get; set; }
        public string CourseId { get; set; }

        public int ScheduleId { get; set; }
        public Course Course { get { return PaulRepository.Courses.First(c => c.Id == CourseId); } }
        public virtual List<SelectedCourseUser> Users { get; set; }
    }

}