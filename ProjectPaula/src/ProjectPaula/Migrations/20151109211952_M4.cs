using System;
using System.Collections.Generic;
using Microsoft.Data.Entity.Migrations;

namespace ProjectPaula.Migrations
{
    public partial class M4 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "Tutorial",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Url", table: "Tutorial");
        }
    }
}
