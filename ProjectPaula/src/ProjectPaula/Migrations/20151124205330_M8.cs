using System;
using System.Collections.Generic;
using Microsoft.Data.Entity.Migrations;

namespace ProjectPaula.Migrations
{
    public partial class M8 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_SelectedCourse_Schedule_ScheduleId", table: "SelectedCourse");
            migrationBuilder.AlterColumn<int>(
                name: "ScheduleId",
                table: "SelectedCourse",
                nullable: false);
            migrationBuilder.AddColumn<string>(
                name: "ShortName",
                table: "Course",
                nullable: true);
            migrationBuilder.AddForeignKey(
                name: "FK_SelectedCourse_Schedule_ScheduleId",
                table: "SelectedCourse",
                column: "ScheduleId",
                principalTable: "Schedule",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_SelectedCourse_Schedule_ScheduleId", table: "SelectedCourse");
            migrationBuilder.DropColumn(name: "ShortName", table: "Course");
            migrationBuilder.AlterColumn<int>(
                name: "ScheduleId",
                table: "SelectedCourse",
                nullable: true);
            migrationBuilder.AddForeignKey(
                name: "FK_SelectedCourse_Schedule_ScheduleId",
                table: "SelectedCourse",
                column: "ScheduleId",
                principalTable: "Schedule",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
