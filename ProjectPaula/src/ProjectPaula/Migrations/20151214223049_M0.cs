using System;
using System.Collections.Generic;
using Microsoft.Data.Entity.Migrations;

namespace ProjectPaula.Migrations
{
    public partial class M0 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CourseCatalog",
                columns: table => new
                {
                    InternalID = table.Column<string>(nullable: false),
                    Title = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseCatalog", x => x.InternalID);
                });
            migrationBuilder.CreateTable(
                name: "Log",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateTime>(nullable: false),
                    Message = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Log", x => x.Id);
                });
            migrationBuilder.CreateTable(
                name: "Course",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    CatalogueInternalID = table.Column<string>(nullable: true),
                    CourseId = table.Column<string>(nullable: true),
                    Docent = table.Column<string>(nullable: true),
                    IsConnectedCourse = table.Column<bool>(nullable: false),
                    IsTutorial = table.Column<bool>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    ShortName = table.Column<string>(nullable: true),
                    Url = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Course", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Course_CourseCatalog_CatalogueInternalID",
                        column: x => x.CatalogueInternalID,
                        principalTable: "CourseCatalog",
                        principalColumn: "InternalID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Course_Course_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Course",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });
            migrationBuilder.CreateTable(
                name: "Schedule",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    CourseCatalogueInternalID = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schedule", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Schedule_CourseCatalog_CourseCatalogueInternalID",
                        column: x => x.CourseCatalogueInternalID,
                        principalTable: "CourseCatalog",
                        principalColumn: "InternalID",
                        onDelete: ReferentialAction.Restrict);
                });
            migrationBuilder.CreateTable(
                name: "ConnectedCourse",
                columns: table => new
                {
                    CourseId = table.Column<string>(nullable: false),
                    CourseId2 = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectedCourse", x => new { x.CourseId, x.CourseId2 });
                    table.ForeignKey(
                        name: "FK_ConnectedCourse_Course_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Course",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConnectedCourse_Course_CourseId2",
                        column: x => x.CourseId2,
                        principalTable: "Course",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });
            migrationBuilder.CreateTable(
                name: "Date",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CourseId = table.Column<string>(nullable: true),
                    From = table.Column<DateTimeOffset>(nullable: false),
                    Instructor = table.Column<string>(nullable: true),
                    Room = table.Column<string>(nullable: true),
                    To = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Date", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Date_Course_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Course",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });
            migrationBuilder.CreateTable(
                name: "SelectedCourse",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CourseId = table.Column<string>(nullable: true),
                    ScheduleId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SelectedCourse", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SelectedCourse_Course_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Course",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SelectedCourse_Schedule_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "Schedule",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });
            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(nullable: true),
                    ScheduleId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                    table.ForeignKey(
                        name: "FK_User_Schedule_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "Schedule",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });
            migrationBuilder.CreateTable(
                name: "SelectedCourseUser",
                columns: table => new
                {
                    UserId = table.Column<int>(nullable: false),
                    SelectedCourseId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SelectedCourseUser", x => new { x.UserId, x.SelectedCourseId });
                    table.ForeignKey(
                        name: "FK_SelectedCourseUser_SelectedCourse_SelectedCourseId",
                        column: x => x.SelectedCourseId,
                        principalTable: "SelectedCourse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SelectedCourseUser_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("ConnectedCourse");
            migrationBuilder.DropTable("Date");
            migrationBuilder.DropTable("Log");
            migrationBuilder.DropTable("SelectedCourseUser");
            migrationBuilder.DropTable("SelectedCourse");
            migrationBuilder.DropTable("User");
            migrationBuilder.DropTable("Course");
            migrationBuilder.DropTable("Schedule");
            migrationBuilder.DropTable("CourseCatalog");
        }
    }
}
