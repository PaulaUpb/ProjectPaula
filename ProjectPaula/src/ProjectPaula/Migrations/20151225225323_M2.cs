using System;
using System.Collections.Generic;
using Microsoft.Data.Entity.Migrations;
using ProjectPaula.Model;

namespace ProjectPaula.Migrations
{
    public partial class M2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Level",
                table: "Log",
                nullable: false,
                defaultValue: FatilityLevel.Normal);
            migrationBuilder.AddColumn<string>(
                name: "Tag",
                table: "Log",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Level", table: "Log");
            migrationBuilder.DropColumn(name: "Tag", table: "Log");
        }
    }
}
