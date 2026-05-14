using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TiffinOS.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeliveryZones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ZoneCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PolygonCoords = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    ColorHex = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryZones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DriverPayoutPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PayoutType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BaseRate = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    BonusPerDelivery = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    BonusThreshold = table.Column<int>(type: "integer", nullable: true),
                    MinGuaranteed = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "INR"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverPayoutPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ReorderThreshold = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuItemCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItemCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Slug = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Module = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    WhatsappNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PlanTier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "starter"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    PrepScheduleTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    PrepScheduleOffsetDays = table.Column<int>(type: "integer", nullable: false),
                    DispatchScheduleTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    DeliveryStartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    DeliveryEndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Timezone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "America/Toronto"),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "CAD"),
                    SmsSenderName = table.Column<string>(type: "text", nullable: true),
                    Settings = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Unit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    MeasurementType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AvailablePortions = table.Column<string>(type: "jsonb", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuItems_MenuItemCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "MenuItemCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ItemSuppliers",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsPreferred = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemSuppliers", x => new { x.ItemId, x.SupplierId });
                    table.ForeignKey(
                        name: "FK_ItemSuppliers_InventoryItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemSuppliers_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsSystemRole = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Roles_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleRunLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RunType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ScheduledFor = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TargetDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RecordsGenerated = table.Column<int>(type: "integer", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleRunLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleRunLogs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantModules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleSlug = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SubscribedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantModules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantModules_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DailyPackingSummaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SummaryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    PortionSize = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TotalQuantity = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    TotalBoxes = table.Column<int>(type: "integer", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyPackingSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyPackingSummaries_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DriverProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LicenceNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MaxDeliveriesPerDay = table.Column<int>(type: "integer", nullable: false),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    PayoutPolicyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverProfiles_DriverPayoutPolicies_PayoutPolicyId",
                        column: x => x.PayoutPolicyId,
                        principalTable: "DriverPayoutPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DriverProfiles_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DriverProfiles_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DriverProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReferenceType = table.Column<string>(type: "text", nullable: true),
                    Payload = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationLogs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotificationLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "draft"),
                    TotalCost = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    OrderedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoggedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LogDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OpeningQty = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    ClosingQty = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    PurchasedQty = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockLogs_InventoryItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockLogs_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockLogs_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TenantHolidays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    HolidayDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    IsDeliveryOff = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantHolidays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantHolidays_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TenantHolidays_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TenantHolidays_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TiffinPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DietaryType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BoxType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TotalDeliveries = table.Column<int>(type: "integer", nullable: true),
                    AvailableFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    AvailableUntil = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    AllowSkip = table.Column<bool>(type: "boolean", nullable: false),
                    MinSkipNoticeHours = table.Column<int>(type: "integer", nullable: true),
                    MaxSkipsPerCycle = table.Column<int>(type: "integer", nullable: true),
                    SkipCreditPolicy = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ExpiryReminderDays = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiffinPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TiffinPlans_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TiffinPlans_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId, x.TenantId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    TokenType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DriverPayoutRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayoutDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PayoutType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TotalDeliveries = table.Column<int>(type: "integer", nullable: false),
                    TotalDistanceKm = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    TotalZones = table.Column<int>(type: "integer", nullable: true),
                    BaseAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    BonusAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverPayoutRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverPayoutRecords_DriverPayoutPolicies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "DriverPayoutPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DriverPayoutRecords_DriverProfiles_DriverId",
                        column: x => x.DriverId,
                        principalTable: "DriverProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DriverPayoutSettlements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    PaymentReference = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    ProcessedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverPayoutSettlements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverPayoutSettlements_DriverProfiles_DriverId",
                        column: x => x.DriverId,
                        principalTable: "DriverProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RouteAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: false),
                    ZoneId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OptimisedSequence = table.Column<string>(type: "jsonb", nullable: false),
                    TotalStops = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    ActualDistanceKm = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteAssignments_DeliveryZones_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "DeliveryZones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RouteAssignments_DriverProfiles_DriverId",
                        column: x => x.DriverId,
                        principalTable: "DriverProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RouteAssignments_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RouteAssignments_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItems_InventoryItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItems_PurchaseOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanPricingTiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    DurationType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PricePerDay = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    BillingCycleDays = table.Column<int>(type: "integer", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanPricingTiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanPricingTiers_TiffinPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "TiffinPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlanPricingTiers_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PlanPricingTiers_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TiffinPlanItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    PortionSize = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiffinPlanItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TiffinPlanItems_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TiffinPlanItems_TiffinPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "TiffinPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DriverLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Lat = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: false),
                    Lng = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: false),
                    AccuracyMetres = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverLocations_DriverProfiles_DriverId",
                        column: x => x.DriverId,
                        principalTable: "DriverProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DriverLocations_RouteAssignments_RouteAssignmentId",
                        column: x => x.RouteAssignmentId,
                        principalTable: "RouteAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryChargeRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ZoneId = table.Column<Guid>(type: "uuid", nullable: true),
                    PricingTierId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChargeType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    MinPlanPricePerDay = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryChargeRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryChargeRules_DeliveryZones_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "DeliveryZones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DeliveryChargeRules_PlanPricingTiers_PricingTierId",
                        column: x => x.PricingTierId,
                        principalTable: "PlanPricingTiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DeliveryChargeRules_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DeliveryChargeRules_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    ZoneId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpicePreference = table.Column<string>(type: "text", nullable: true),
                    DeliveryAddress = table.Column<string>(type: "text", nullable: false),
                    DeliveryLat = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: false),
                    DeliveryLng = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: false),
                    FloorOrUnit = table.Column<string>(type: "text", nullable: true),
                    DeliveryInstructions = table.Column<string>(type: "text", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    PausedUntil = table.Column<DateOnly>(type: "date", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "text", nullable: true),
                    PricingTierId = table.Column<Guid>(type: "uuid", nullable: false),
                    LockedPricePerDay = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    LockedTotalAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    DeliveryChargeRuleId = table.Column<Guid>(type: "uuid", nullable: true),
                    LockedDeliveryCharge = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subscriptions_DeliveryChargeRules_DeliveryChargeRuleId",
                        column: x => x.DeliveryChargeRuleId,
                        principalTable: "DeliveryChargeRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Subscriptions_DeliveryZones_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "DeliveryZones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Subscriptions_PlanPricingTiers_PricingTierId",
                        column: x => x.PricingTierId,
                        principalTable: "PlanPricingTiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Subscriptions_TiffinPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "TiffinPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Subscriptions_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DeliverySchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteAssignmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScheduledDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SequenceNumber = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    AttemptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "text", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliverySchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliverySchedules_DriverProfiles_DriverId",
                        column: x => x.DriverId,
                        principalTable: "DriverProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DeliverySchedules_RouteAssignments_RouteAssignmentId",
                        column: x => x.RouteAssignmentId,
                        principalTable: "RouteAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DeliverySchedules_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeliverySchedules_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DeliverySchedules_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DeliverySkipLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkippedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreditPolicy = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RequestedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliverySkipLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliverySkipLogs_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "INR"),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    Gateway = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    GatewayRef = table.Column<string>(type: "text", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "text", nullable: false),
                    FailureReason = table.Column<string>(type: "text", nullable: true),
                    RefundedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PODRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    DeliveryId = table.Column<Guid>(type: "uuid", nullable: false),
                    PhotoUrl = table.Column<string>(type: "text", nullable: false),
                    CaptureLat = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: false),
                    CaptureLng = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: false),
                    CaptureAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UploadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "uploaded")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PODRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PODRecords_DeliverySchedules_DeliveryId",
                        column: x => x.DeliveryId,
                        principalTable: "DeliverySchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionRenewals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PricingTierId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    AmountCharged = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    DeliveryCharge = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    PaymentTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReminderSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionRenewals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionRenewals_PaymentTransactions_PaymentTransaction~",
                        column: x => x.PaymentTransactionId,
                        principalTable: "PaymentTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SubscriptionRenewals_PlanPricingTiers_PricingTierId",
                        column: x => x.PricingTierId,
                        principalTable: "PlanPricingTiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubscriptionRenewals_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "CreatedAt", "Description", "Module", "Name", "Slug" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000001"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "platform", "Manage Tenants", "platform:tenants:manage" },
                    { new Guid("00000000-0000-0000-0000-000000000002"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "platform", "Manage Billing", "platform:billing:manage" },
                    { new Guid("00000000-0000-0000-0000-000000000003"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "tiffin", "Read Plans", "tiffin:plans:read" },
                    { new Guid("00000000-0000-0000-0000-000000000004"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "tiffin", "Write Plans", "tiffin:plans:write" },
                    { new Guid("00000000-0000-0000-0000-000000000005"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "tiffin", "Archive Plans", "tiffin:plans:archive" },
                    { new Guid("00000000-0000-0000-0000-000000000006"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "tiffin", "Read All Subscriptions", "tiffin:subscriptions:read:all" },
                    { new Guid("00000000-0000-0000-0000-000000000007"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "tiffin", "Read Own Subscription", "tiffin:subscriptions:read:own" },
                    { new Guid("00000000-0000-0000-0000-000000000008"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "tiffin", "Write Subscriptions", "tiffin:subscriptions:write" },
                    { new Guid("00000000-0000-0000-0000-000000000009"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "tiffin", "Cancel Subscriptions", "tiffin:subscriptions:cancel" },
                    { new Guid("00000000-0000-0000-0000-000000000010"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "tiffin", "Read Deliveries", "tiffin:deliveries:read" },
                    { new Guid("00000000-0000-0000-0000-000000000011"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "tiffin", "Assign Deliveries", "tiffin:deliveries:assign" },
                    { new Guid("00000000-0000-0000-0000-000000000012"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "tiffin", "Update Delivery Status", "tiffin:deliveries:status:update" },
                    { new Guid("00000000-0000-0000-0000-000000000013"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "tiffin", "Upload POD", "tiffin:deliveries:pod:upload" },
                    { new Guid("00000000-0000-0000-0000-000000000014"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "tiffin", "Track Own Delivery", "tiffin:deliveries:track:own" },
                    { new Guid("00000000-0000-0000-0000-000000000015"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "tiffin", "Manage Drivers", "tiffin:drivers:manage" },
                    { new Guid("00000000-0000-0000-0000-000000000016"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "tiffin", "View Driver Location", "tiffin:drivers:location:view" },
                    { new Guid("00000000-0000-0000-0000-000000000017"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "tiffin", "View Routes", "tiffin:routes:view" },
                    { new Guid("00000000-0000-0000-0000-000000000018"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "tiffin", "View Packing Summary", "tiffin:packing:summary:view" },
                    { new Guid("00000000-0000-0000-0000-000000000019"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "tiffin", "Confirm Packing Summary", "tiffin:packing:summary:confirm" },
                    { new Guid("00000000-0000-0000-0000-000000000020"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "tiffin", "View Delivery Reports", "tiffin:reports:delivery" },
                    { new Guid("00000000-0000-0000-0000-000000000021"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "tiffin", "View Revenue Reports", "tiffin:reports:revenue" },
                    { new Guid("00000000-0000-0000-0000-000000000022"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "inventory", "Manage Inventory Items", "inventory:items:manage" },
                    { new Guid("00000000-0000-0000-0000-000000000023"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "inventory", "Log Stock", "inventory:stock:log" },
                    { new Guid("00000000-0000-0000-0000-000000000024"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "inventory", "View Stock", "inventory:stock:view" },
                    { new Guid("00000000-0000-0000-0000-000000000025"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "inventory", "Manage Suppliers", "inventory:suppliers:manage" },
                    { new Guid("00000000-0000-0000-0000-000000000026"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "inventory", "Manage Purchase Orders", "inventory:orders:manage" }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedAt", "Description", "IsSystemRole", "Name", "Slug", "TenantId" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Full platform access", true, "Super Admin", "super_admin", null },
                    { new Guid("10000000-0000-0000-0000-000000000002"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Full access within their tenant", true, "Tenant Admin", "tenant_admin", null },
                    { new Guid("10000000-0000-0000-0000-000000000003"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Operational access, no billing", true, "Manager", "manager", null },
                    { new Guid("10000000-0000-0000-0000-000000000004"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Kitchen packing access only", true, "Cook", "cook", null },
                    { new Guid("10000000-0000-0000-0000-000000000005"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Delivery route and POD only", true, "Driver", "driver", null },
                    { new Guid("10000000-0000-0000-0000-000000000006"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Own subscription and tracking only", true, "Customer", "customer", null }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("00000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("00000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("00000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("00000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("00000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("00000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("00000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("00000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("00000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("00000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("00000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("00000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("00000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("00000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("00000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("00000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("00000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("00000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("00000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("00000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("00000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("00000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("00000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("00000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("00000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("00000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("00000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("00000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("00000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("00000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("00000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("00000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("00000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("00000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("00000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("00000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("00000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("00000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("00000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("00000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("00000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("00000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("00000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("00000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("00000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("00000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("00000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("00000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("00000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("00000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("00000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("00000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("00000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("00000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("00000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("00000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("00000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("00000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("00000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("00000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("00000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("00000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("00000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("00000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("00000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("00000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("00000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("00000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000004") },
                    { new Guid("00000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000004") },
                    { new Guid("00000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000004") },
                    { new Guid("00000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000004") },
                    { new Guid("00000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000005") },
                    { new Guid("00000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000005") },
                    { new Guid("00000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000005") },
                    { new Guid("00000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000005") },
                    { new Guid("00000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000006") },
                    { new Guid("00000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000006") },
                    { new Guid("00000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000006") },
                    { new Guid("00000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000006") },
                    { new Guid("00000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000006") },
                    { new Guid("00000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000006") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyPackingSummaries_MenuItemId",
                table: "DailyPackingSummaries",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyPackingSummaries_TenantId_SummaryDate_MenuItemId_Porti~",
                table: "DailyPackingSummaries",
                columns: new[] { "TenantId", "SummaryDate", "MenuItemId", "PortionSize" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryChargeRules_CreatedBy",
                table: "DeliveryChargeRules",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryChargeRules_PricingTierId",
                table: "DeliveryChargeRules",
                column: "PricingTierId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryChargeRules_TenantId_Priority",
                table: "DeliveryChargeRules",
                columns: new[] { "TenantId", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryChargeRules_UpdatedBy",
                table: "DeliveryChargeRules",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryChargeRules_ZoneId",
                table: "DeliveryChargeRules",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliverySchedules_CreatedBy",
                table: "DeliverySchedules",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DeliverySchedules_DriverId",
                table: "DeliverySchedules",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliverySchedules_RouteAssignmentId",
                table: "DeliverySchedules",
                column: "RouteAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliverySchedules_SubscriptionId_ScheduledDate",
                table: "DeliverySchedules",
                columns: new[] { "SubscriptionId", "ScheduledDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliverySchedules_TenantId_ScheduledDate_Status",
                table: "DeliverySchedules",
                columns: new[] { "TenantId", "ScheduledDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliverySchedules_UpdatedBy",
                table: "DeliverySchedules",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DeliverySkipLogs_SubscriptionId_SkippedDate",
                table: "DeliverySkipLogs",
                columns: new[] { "SubscriptionId", "SkippedDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryZones_TenantId_ZoneCode",
                table: "DeliveryZones",
                columns: new[] { "TenantId", "ZoneCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriverLocations_DriverId_RecordedAt",
                table: "DriverLocations",
                columns: new[] { "DriverId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DriverLocations_RouteAssignmentId",
                table: "DriverLocations",
                column: "RouteAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverPayoutRecords_DriverId_PayoutDate",
                table: "DriverPayoutRecords",
                columns: new[] { "DriverId", "PayoutDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriverPayoutRecords_PolicyId",
                table: "DriverPayoutRecords",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverPayoutSettlements_DriverId_PeriodStart_PeriodEnd",
                table: "DriverPayoutSettlements",
                columns: new[] { "DriverId", "PeriodStart", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_DriverProfiles_CreatedBy",
                table: "DriverProfiles",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DriverProfiles_PayoutPolicyId",
                table: "DriverProfiles",
                column: "PayoutPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverProfiles_TenantId_UserId",
                table: "DriverProfiles",
                columns: new[] { "TenantId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriverProfiles_UpdatedBy",
                table: "DriverProfiles",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DriverProfiles_UserId",
                table: "DriverProfiles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_TenantId_Name",
                table: "InventoryItems",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemSuppliers_SupplierId",
                table: "ItemSuppliers",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemCategories_TenantId_Name",
                table: "MenuItemCategories",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_CategoryId",
                table: "MenuItems",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_TenantId_Name",
                table: "MenuItems",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_ReferenceId",
                table: "NotificationLogs",
                column: "ReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_TenantId_UserId",
                table: "NotificationLogs",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_UserId",
                table: "NotificationLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_GatewayRef",
                table: "PaymentTransactions",
                column: "GatewayRef",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_IdempotencyKey",
                table: "PaymentTransactions",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_SubscriptionId",
                table: "PaymentTransactions",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_TenantId_Status",
                table: "PaymentTransactions",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Slug",
                table: "Permissions",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanPricingTiers_CreatedBy",
                table: "PlanPricingTiers",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PlanPricingTiers_PlanId_DurationType",
                table: "PlanPricingTiers",
                columns: new[] { "PlanId", "DurationType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanPricingTiers_UpdatedBy",
                table: "PlanPricingTiers",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PODRecords_DeliveryId",
                table: "PODRecords",
                column: "DeliveryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_ItemId",
                table: "PurchaseOrderItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_OrderId",
                table: "PurchaseOrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_CreatedBy",
                table: "PurchaseOrders",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierId",
                table: "PurchaseOrders",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_TenantId_Status",
                table: "PurchaseOrders",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_UpdatedBy",
                table: "PurchaseOrders",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Slug",
                table: "Roles",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_TenantId",
                table: "Roles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteAssignments_CreatedBy",
                table: "RouteAssignments",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RouteAssignments_DriverId_AssignmentDate_ZoneId",
                table: "RouteAssignments",
                columns: new[] { "DriverId", "AssignmentDate", "ZoneId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RouteAssignments_UpdatedBy",
                table: "RouteAssignments",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RouteAssignments_ZoneId",
                table: "RouteAssignments",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleRunLogs_TenantId_RunType_TargetDate",
                table: "ScheduleRunLogs",
                columns: new[] { "TenantId", "RunType", "TargetDate" });

            migrationBuilder.CreateIndex(
                name: "IX_StockLogs_CreatedBy",
                table: "StockLogs",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_StockLogs_ItemId",
                table: "StockLogs",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLogs_TenantId_ItemId_LogDate",
                table: "StockLogs",
                columns: new[] { "TenantId", "ItemId", "LogDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockLogs_UpdatedBy",
                table: "StockLogs",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionRenewals_PaymentTransactionId",
                table: "SubscriptionRenewals",
                column: "PaymentTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionRenewals_PricingTierId",
                table: "SubscriptionRenewals",
                column: "PricingTierId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionRenewals_SubscriptionId_PeriodStart",
                table: "SubscriptionRenewals",
                columns: new[] { "SubscriptionId", "PeriodStart" });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_CreatedBy",
                table: "Subscriptions",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_DeliveryChargeRuleId",
                table: "Subscriptions",
                column: "DeliveryChargeRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PlanId",
                table: "Subscriptions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PricingTierId",
                table: "Subscriptions",
                column: "PricingTierId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_TenantId_CustomerId",
                table: "Subscriptions",
                columns: new[] { "TenantId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_TenantId_Status",
                table: "Subscriptions",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_UpdatedBy",
                table: "Subscriptions",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_ZoneId",
                table: "Subscriptions",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantHolidays_CreatedBy",
                table: "TenantHolidays",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TenantHolidays_TenantId_HolidayDate",
                table: "TenantHolidays",
                columns: new[] { "TenantId", "HolidayDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantHolidays_UpdatedBy",
                table: "TenantHolidays",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TenantModules_TenantId_ModuleSlug",
                table: "TenantModules",
                columns: new[] { "TenantId", "ModuleSlug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Slug",
                table: "Tenants",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TiffinPlanItems_MenuItemId",
                table: "TiffinPlanItems",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TiffinPlanItems_PlanId_MenuItemId",
                table: "TiffinPlanItems",
                columns: new[] { "PlanId", "MenuItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TiffinPlans_CreatedBy",
                table: "TiffinPlans",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TiffinPlans_UpdatedBy",
                table: "TiffinPlans",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_TenantId",
                table: "UserRoles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId_Email",
                table: "Users",
                columns: new[] { "TenantId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserTokens_Token",
                table: "UserTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserTokens_UserId",
                table: "UserTokens",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyPackingSummaries");

            migrationBuilder.DropTable(
                name: "DeliverySkipLogs");

            migrationBuilder.DropTable(
                name: "DriverLocations");

            migrationBuilder.DropTable(
                name: "DriverPayoutRecords");

            migrationBuilder.DropTable(
                name: "DriverPayoutSettlements");

            migrationBuilder.DropTable(
                name: "ItemSuppliers");

            migrationBuilder.DropTable(
                name: "NotificationLogs");

            migrationBuilder.DropTable(
                name: "PODRecords");

            migrationBuilder.DropTable(
                name: "PurchaseOrderItems");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "ScheduleRunLogs");

            migrationBuilder.DropTable(
                name: "StockLogs");

            migrationBuilder.DropTable(
                name: "SubscriptionRenewals");

            migrationBuilder.DropTable(
                name: "TenantHolidays");

            migrationBuilder.DropTable(
                name: "TenantModules");

            migrationBuilder.DropTable(
                name: "TiffinPlanItems");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "UserTokens");

            migrationBuilder.DropTable(
                name: "DeliverySchedules");

            migrationBuilder.DropTable(
                name: "PurchaseOrders");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "InventoryItems");

            migrationBuilder.DropTable(
                name: "PaymentTransactions");

            migrationBuilder.DropTable(
                name: "MenuItems");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "RouteAssignments");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "MenuItemCategories");

            migrationBuilder.DropTable(
                name: "DriverProfiles");

            migrationBuilder.DropTable(
                name: "DeliveryChargeRules");

            migrationBuilder.DropTable(
                name: "DriverPayoutPolicies");

            migrationBuilder.DropTable(
                name: "DeliveryZones");

            migrationBuilder.DropTable(
                name: "PlanPricingTiers");

            migrationBuilder.DropTable(
                name: "TiffinPlans");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Tenants");
        }
    }
}
