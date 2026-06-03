using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectary.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PresetId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Presets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ParentPresetId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Presets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Presets_Presets_ParentPresetId",
                        column: x => x.ParentPresetId,
                        principalTable: "Presets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BoolFieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ShowInList = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoolFieldDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BoolFieldValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Value = table.Column<bool>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoolFieldValues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ColorFieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Format = table.Column<int>(type: "INTEGER", nullable: false),
                    ShowInList = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ColorFieldDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ColorFieldValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ColorFieldValues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DateFieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Min = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Max = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ShowInList = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DateFieldDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DateFieldValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Value = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DateFieldValues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DecimalFieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DecimalPlaces = table.Column<int>(type: "INTEGER", nullable: false),
                    ShowInList = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecimalFieldDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DecimalFieldValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Value = table.Column<decimal>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecimalFieldValues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DisplayNameFieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ShowInList = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisplayNameFieldDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PresetId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ParentListFieldDefinitionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Label = table.Column<string>(type: "TEXT", nullable: false),
                    IsRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldDefinitions_Presets_PresetId",
                        column: x => x.PresetId,
                        principalTable: "Presets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImageFieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageFieldDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImageFieldDefinitions_FieldDefinitions_Id",
                        column: x => x.Id,
                        principalTable: "FieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntegerFieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Min = table.Column<int>(type: "INTEGER", nullable: true),
                    Max = table.Column<int>(type: "INTEGER", nullable: true),
                    ShowInList = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegerFieldDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegerFieldDefinitions_FieldDefinitions_Id",
                        column: x => x.Id,
                        principalTable: "FieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ListFieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InlineStyle = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListFieldDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListFieldDefinitions_FieldDefinitions_Id",
                        column: x => x.Id,
                        principalTable: "FieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MultiChoiceFieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ShowInList = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MultiChoiceFieldDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MultiChoiceFieldDefinitions_FieldDefinitions_Id",
                        column: x => x.Id,
                        principalTable: "FieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RatingFieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MaxStars = table.Column<int>(type: "INTEGER", nullable: false),
                    ShowInList = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RatingFieldDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RatingFieldDefinitions_FieldDefinitions_Id",
                        column: x => x.Id,
                        principalTable: "FieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SingleChoiceFieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ShowInList = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SingleChoiceFieldDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SingleChoiceFieldDefinitions_FieldDefinitions_Id",
                        column: x => x.Id,
                        principalTable: "FieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TextFieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MaxLength = table.Column<int>(type: "INTEGER", nullable: true),
                    ShowInList = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TextFieldDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TextFieldDefinitions_FieldDefinitions_Id",
                        column: x => x.Id,
                        principalTable: "FieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UrlFieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ShowInList = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UrlFieldDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UrlFieldDefinitions_FieldDefinitions_Id",
                        column: x => x.Id,
                        principalTable: "FieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MultiChoiceOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    MultiChoiceFieldDefinitionId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MultiChoiceOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MultiChoiceOptions_MultiChoiceFieldDefinitions_MultiChoiceFieldDefinitionId",
                        column: x => x.MultiChoiceFieldDefinitionId,
                        principalTable: "MultiChoiceFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SingleChoiceOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    SingleChoiceFieldDefinitionId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SingleChoiceOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SingleChoiceOptions_SingleChoiceFieldDefinitions_SingleChoiceFieldDefinitionId",
                        column: x => x.SingleChoiceFieldDefinitionId,
                        principalTable: "SingleChoiceFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FieldValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FieldDefinitionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ListEntryId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldValues_FieldDefinitions_FieldDefinitionId",
                        column: x => x.FieldDefinitionId,
                        principalTable: "FieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FieldValues_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImageFieldValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ImageKey = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImageFieldValues_FieldValues_Id",
                        column: x => x.Id,
                        principalTable: "FieldValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntegerFieldValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Value = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegerFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegerFieldValues_FieldValues_Id",
                        column: x => x.Id,
                        principalTable: "FieldValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ListFieldValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListFieldValues_FieldValues_Id",
                        column: x => x.Id,
                        principalTable: "FieldValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MultiChoiceFieldValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Selected = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MultiChoiceFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MultiChoiceFieldValues_FieldValues_Id",
                        column: x => x.Id,
                        principalTable: "FieldValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RatingFieldValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Stars = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RatingFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RatingFieldValues_FieldValues_Id",
                        column: x => x.Id,
                        principalTable: "FieldValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SingleChoiceFieldValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Selected = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SingleChoiceFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SingleChoiceFieldValues_FieldValues_Id",
                        column: x => x.Id,
                        principalTable: "FieldValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TextFieldValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TextFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TextFieldValues_FieldValues_Id",
                        column: x => x.Id,
                        principalTable: "FieldValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UrlFieldValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UrlFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UrlFieldValues_FieldValues_Id",
                        column: x => x.Id,
                        principalTable: "FieldValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ListEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ListFieldValueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListEntries_ListFieldValues_ListFieldValueId",
                        column: x => x.ListFieldValueId,
                        principalTable: "ListFieldValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FieldDefinitions_ParentListFieldDefinitionId",
                table: "FieldDefinitions",
                column: "ParentListFieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldDefinitions_PresetId",
                table: "FieldDefinitions",
                column: "PresetId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldValues_FieldDefinitionId",
                table: "FieldValues",
                column: "FieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldValues_ItemId",
                table: "FieldValues",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldValues_ListEntryId",
                table: "FieldValues",
                column: "ListEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_ListEntries_ListFieldValueId",
                table: "ListEntries",
                column: "ListFieldValueId");

            migrationBuilder.CreateIndex(
                name: "IX_MultiChoiceOptions_MultiChoiceFieldDefinitionId",
                table: "MultiChoiceOptions",
                column: "MultiChoiceFieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_Presets_ParentPresetId",
                table: "Presets",
                column: "ParentPresetId");

            migrationBuilder.CreateIndex(
                name: "IX_SingleChoiceOptions_SingleChoiceFieldDefinitionId",
                table: "SingleChoiceOptions",
                column: "SingleChoiceFieldDefinitionId");

            migrationBuilder.AddForeignKey(
                name: "FK_BoolFieldDefinitions_FieldDefinitions_Id",
                table: "BoolFieldDefinitions",
                column: "Id",
                principalTable: "FieldDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BoolFieldValues_FieldValues_Id",
                table: "BoolFieldValues",
                column: "Id",
                principalTable: "FieldValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ColorFieldDefinitions_FieldDefinitions_Id",
                table: "ColorFieldDefinitions",
                column: "Id",
                principalTable: "FieldDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ColorFieldValues_FieldValues_Id",
                table: "ColorFieldValues",
                column: "Id",
                principalTable: "FieldValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DateFieldDefinitions_FieldDefinitions_Id",
                table: "DateFieldDefinitions",
                column: "Id",
                principalTable: "FieldDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DateFieldValues_FieldValues_Id",
                table: "DateFieldValues",
                column: "Id",
                principalTable: "FieldValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DecimalFieldDefinitions_FieldDefinitions_Id",
                table: "DecimalFieldDefinitions",
                column: "Id",
                principalTable: "FieldDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DecimalFieldValues_FieldValues_Id",
                table: "DecimalFieldValues",
                column: "Id",
                principalTable: "FieldValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DisplayNameFieldDefinitions_FieldDefinitions_Id",
                table: "DisplayNameFieldDefinitions",
                column: "Id",
                principalTable: "FieldDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FieldDefinitions_ListFieldDefinitions_ParentListFieldDefinitionId",
                table: "FieldDefinitions",
                column: "ParentListFieldDefinitionId",
                principalTable: "ListFieldDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FieldValues_ListEntries_ListEntryId",
                table: "FieldValues",
                column: "ListEntryId",
                principalTable: "ListEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FieldValues_FieldDefinitions_FieldDefinitionId",
                table: "FieldValues");

            migrationBuilder.DropForeignKey(
                name: "FK_ListFieldDefinitions_FieldDefinitions_Id",
                table: "ListFieldDefinitions");

            migrationBuilder.DropForeignKey(
                name: "FK_ListFieldValues_FieldValues_Id",
                table: "ListFieldValues");

            migrationBuilder.DropTable(
                name: "BoolFieldDefinitions");

            migrationBuilder.DropTable(
                name: "BoolFieldValues");

            migrationBuilder.DropTable(
                name: "ColorFieldDefinitions");

            migrationBuilder.DropTable(
                name: "ColorFieldValues");

            migrationBuilder.DropTable(
                name: "DateFieldDefinitions");

            migrationBuilder.DropTable(
                name: "DateFieldValues");

            migrationBuilder.DropTable(
                name: "DecimalFieldDefinitions");

            migrationBuilder.DropTable(
                name: "DecimalFieldValues");

            migrationBuilder.DropTable(
                name: "DisplayNameFieldDefinitions");

            migrationBuilder.DropTable(
                name: "ImageFieldDefinitions");

            migrationBuilder.DropTable(
                name: "ImageFieldValues");

            migrationBuilder.DropTable(
                name: "IntegerFieldDefinitions");

            migrationBuilder.DropTable(
                name: "IntegerFieldValues");

            migrationBuilder.DropTable(
                name: "MultiChoiceFieldValues");

            migrationBuilder.DropTable(
                name: "MultiChoiceOptions");

            migrationBuilder.DropTable(
                name: "RatingFieldDefinitions");

            migrationBuilder.DropTable(
                name: "RatingFieldValues");

            migrationBuilder.DropTable(
                name: "SingleChoiceFieldValues");

            migrationBuilder.DropTable(
                name: "SingleChoiceOptions");

            migrationBuilder.DropTable(
                name: "TextFieldDefinitions");

            migrationBuilder.DropTable(
                name: "TextFieldValues");

            migrationBuilder.DropTable(
                name: "UrlFieldDefinitions");

            migrationBuilder.DropTable(
                name: "UrlFieldValues");

            migrationBuilder.DropTable(
                name: "MultiChoiceFieldDefinitions");

            migrationBuilder.DropTable(
                name: "SingleChoiceFieldDefinitions");

            migrationBuilder.DropTable(
                name: "FieldDefinitions");

            migrationBuilder.DropTable(
                name: "ListFieldDefinitions");

            migrationBuilder.DropTable(
                name: "Presets");

            migrationBuilder.DropTable(
                name: "FieldValues");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "ListEntries");

            migrationBuilder.DropTable(
                name: "ListFieldValues");
        }
    }
}
