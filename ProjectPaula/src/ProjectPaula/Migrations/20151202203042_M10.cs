using System;
using System.Collections.Generic;
using Microsoft.Data.Entity.Migrations;

namespace ProjectPaula.Migrations
{
    public partial class M10 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_SelectedCourse_Schedule_ScheduleId", table: "SelectedCourse");
            migrationBuilder.DropForeignKey(name: "FK_User_Schedule_ScheduleId", table: "User");
            migrationBuilder.AddColumn<bool>(
                name: "IsConnectedCourse",
                table: "Course",
                nullable: false,
                defaultValue: false);
            migrationBuilder.AddForeignKey(
                name: "FK_SelectedCourse_Schedule_ScheduleId",
                table: "SelectedCourse",
                column: "ScheduleId",
                principalTable: "Schedule",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(
                name: "FK_User_Schedule_ScheduleId",
                table: "User",
                column: "ScheduleId",
                principalTable: "Schedule",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_SelectedCourse_Schedule_ScheduleId", table: "SelectedCourse");
            migrationBuilder.DropForeignKey(name: "FK_User_Schedule_ScheduleId", table: "User");
            migrationBuilder.DropColumn(name: "IsConnectedCourse", table: "Course");
            migrationBuilder.AddForeignKey(
                name: "FK_SelectedCourse_Schedule_ScheduleId",
                table: "SelectedCourse",
                column: "ScheduleId",
                principalTable: "Schedule",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(
                name: "FK_User_Schedule_ScheduleId",
                table: "User",
                column: "ScheduleId",
                principalTable: "Schedule",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
