using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace ProjectPaula.Model
{
    public class Schedule
    {

        public string Id { get; set; }

        /// <summary>
        /// List of users participating in this Schedule
        /// </summary>
        public virtual List<User> User { get; set; } = new List<User>();

        public virtual List<SelectedCourse> SelectedCourses { get; } = new List<SelectedCourse>();


        public virtual CourseCatalog CourseCatalogue { get; set; }


        /// <summary>
        /// Add a course to this schedule. This automatically updates the <see cref="DatesByDay"/> and calls <see cref="RecalculateTimes"/>.
        /// </summary>
        /// <param name="selectedCourse"></param>
        public void AddCourse(SelectedCourse selectedCourse)
        {
            SelectedCourses.Add(selectedCourse);
        }

        /// <summary>
        /// Remove a course from this schedule. This automatically updates the <see cref="DatesByDay"/> and calls <see cref="RecalculateTimes"/>.
        /// </summary>
        /// <param name="selectedCourse"></param>
        public void RemoveCourse(SelectedCourse selectedCourse)
        {
            SelectedCourses.Remove(selectedCourse);
        }

        /// <summary>
        /// Remove a course from this schedule. This automatically updates the <see cref="DatesByDay"/> and calls <see cref="RecalculateTimes"/>.
        /// </summary>
        /// <param name="selectedCourse"></param>
        public void RemoveCourse(string courseId)
        {
            var course = SelectedCourses.FirstOrDefault(selectedCourse => selectedCourse.CourseId == courseId);
            RemoveCourse(course);
        }
    }
}
