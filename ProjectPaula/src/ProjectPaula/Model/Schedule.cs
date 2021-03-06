﻿using System.Collections.Generic;
using System.Linq;

namespace ProjectPaula.Model
{
    public class Schedule
    {
        public string Id { get; set; }

        public string Name { get; set; }

        /// <summary>
        /// List of users participating in this Schedule.
        /// </summary>
        public virtual List<User> Users { get; set; } = new List<User>();

        public virtual List<SelectedCourse> SelectedCourses { get; } = new List<SelectedCourse>();

        public virtual CourseCatalog CourseCatalogue { get; set; }

        /// <summary>
        /// Adds courses to this schedule.
        /// </summary>
        /// <param name="selectedCourse"></param>
        public void AddCourses(ICollection<SelectedCourse> selectedCourse)
        {
            SelectedCourses.AddRange(selectedCourse);
        }

        /// <summary>
        /// Removes a course from this schedule.
        /// </summary>
        /// <param name="selectedCourse"></param>
        public void RemoveCourse(SelectedCourse selectedCourse)
        {
            SelectedCourses.Remove(selectedCourse);
        }

        /// <summary>
        /// Removes a course from this schedule.
        /// </summary>
        /// <param name="courseId"></param>
        public void RemoveCourse(string courseId)
        {
            var course = SelectedCourses.FirstOrDefault(selectedCourse => selectedCourse.CourseId == courseId);
            RemoveCourse(course);
        }
    }
}
