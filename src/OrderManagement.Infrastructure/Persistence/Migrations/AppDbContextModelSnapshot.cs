using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using OrderManagement.Infrastructure.Persistence;

#nullable disable

namespace OrderManagement.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
partial class AppDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.5")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

        modelBuilder.Entity("OrderManagement.Domain.Entities.Order", b =>
        {
            b.Property<Guid>("Id")
                .HasColumnType("uuid");

            b.Property<DateTimeOffset>("CreatedAt")
                .HasColumnType("timestamp with time zone");

            b.Property<string>("Customer")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("character varying(200)");

            b.Property<string>("Product")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("character varying(200)");

            b.Property<int>("Status")
                .HasColumnType("integer");

            b.Property<DateTimeOffset?>("UpdatedAt")
                .HasColumnType("timestamp with time zone");

            b.Property<decimal>("Value")
                .HasColumnType("numeric(18,2)");

            b.HasKey("Id");

            b.ToTable("orders", (string)null);
        });

        modelBuilder.Entity("OrderManagement.Domain.Entities.OrderStatusHistory", b =>
        {
            b.Property<Guid>("Id")
                .HasColumnType("uuid");

            b.Property<DateTimeOffset>("ChangedAt")
                .HasColumnType("timestamp with time zone");

            b.Property<int>("NewStatus")
                .HasColumnType("integer");

            b.Property<Guid>("OrderId")
                .HasColumnType("uuid");

            b.Property<int?>("PreviousStatus")
                .HasColumnType("integer");

            b.Property<string>("Source")
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("character varying(50)");

            b.HasKey("Id");

            b.HasIndex("OrderId", "ChangedAt");

            b.ToTable("order_status_history", (string)null);
        });

        modelBuilder.Entity("OrderManagement.Domain.Entities.ProcessedMessage", b =>
        {
            b.Property<Guid>("Id")
                .HasColumnType("uuid");

            b.Property<string>("CorrelationId")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("character varying(200)");

            b.Property<string>("EventType")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)");

            b.Property<string>("MessageId")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("character varying(200)");

            b.Property<DateTimeOffset>("ProcessedAt")
                .HasColumnType("timestamp with time zone");

            b.HasKey("Id");

            b.HasIndex("MessageId")
                .IsUnique();

            b.ToTable("processed_messages", (string)null);
        });

        modelBuilder.Entity("OrderManagement.Domain.Entities.OutboxMessage", b =>
        {
            b.Property<Guid>("Id")
                .HasColumnType("uuid");

            b.Property<string>("CorrelationId")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("character varying(200)");

            b.Property<DateTimeOffset>("CreatedAt")
                .HasColumnType("timestamp with time zone");

            b.Property<string>("EventType")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)");

            b.Property<string>("MessageId")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("character varying(200)");

            b.Property<string>("Payload")
                .IsRequired()
                .HasColumnType("jsonb");

            b.Property<DateTimeOffset?>("ProcessedAt")
                .HasColumnType("timestamp with time zone");

            b.HasKey("Id");

            b.HasIndex("EventType", "ProcessedAt", "CreatedAt");

            b.HasIndex("MessageId")
                .IsUnique();

            b.ToTable("outbox_messages", (string)null);
        });

        modelBuilder.Entity("OrderManagement.Domain.Entities.OrderStatusHistory", b =>
        {
            b.HasOne("OrderManagement.Domain.Entities.Order", "Order")
                .WithMany("StatusHistory")
                .HasForeignKey("OrderId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("Order");
        });

        modelBuilder.Entity("OrderManagement.Domain.Entities.Order", b =>
        {
            b.Navigation("StatusHistory");
        });
    }
}
