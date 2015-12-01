using System;
using System.Collections.Generic;
using Microsoft.Data.Entity.Migrations;

namespace ProjectPaula.Migrations
{
    public partial class M9 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_SelectedCourse_Schedule_ScheduleId", table: "SelectedCourse");
            migrationBuilder.DropForeignKey(name: "FK_User_Schedule_ScheduleId", table: "User");
            migrationBuilder.AlterColumn<int>(
                name: "ScheduleId",
                table: "User",
                nullable: false);
            migrationBuilder.AddColumn<string>(
                name: "CourseCatalogueInternalID",
                table: "Schedule",
                nullable: true);
            migrationBuilder.AddForeignKey(
                name: "FK_Schedule_CourseCatalogue_CourseCatalogueInternalID",
                table: "Schedule",
                column: "CourseCatalogueInternalID",
                principalTable: "CourseCatalogue",
                principalColumn: "InternalID",
                onDelete: ReferentialAction.Restrict);
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
            migrationBuilder.DropForeignKey(name: "FK_Schedule_CourseCatalogue_CourseCatalogueInternalID", table: "Schedule");
            migrationBuilder.DropForeignKey(name: "FK_SelectedCourse_Schedule_ScheduleId", table: "SelectedCourse");
            migrationBuilder.DropForeignKey(name: "FK_User_Schedule_ScheduleId", table: "User");
            migrationBuilder.DropColumn(name: "CourseCatalogueInternalID", table: "Schedule");
            migrationBuilder.AlterColumn<int>(
                name: "ScheduleId",
                table: "User",
                nullable: true);
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
