using System;
using System.Collections.Generic;
using Microsoft.Data.Entity.Migrations;

namespace ProjectPaula.Migrations
{
    public partial class M7 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(name: "PK_SelectedCourseUser", table: "SelectedCourseUser");
            migrationBuilder.AddPrimaryKey(
                name: "PK_SelectedCourseUser",
                table: "SelectedCourseUser",
                columns: new[] { "UserId", "SelectedCourseId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(name: "PK_SelectedCourseUser", table: "SelectedCourseUser");
            migrationBuilder.AddPrimaryKey(
                name: "PK_SelectedCourseUser",
                table: "SelectedCourseUser",
                columns: new[] { "SelectedCourseId", "UserId" });
        }
    }
}
