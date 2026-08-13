using System;

namespace CognitiveLedger.Data.Models.Base;

public interface IIdentifiable
{
    long Id { get; set; }
}

public interface IAuditable
{
    DateTime CreatedAtUtc { get; set; }
    string? CreatedBy { get; set; }

    DateTime? UpdatedAtUtc { get; set; }
    string? UpdatedBy { get; set; }
}

public interface IDeletable
{
    bool IsDeleted { get; set; }
}

public interface IActivatable
{
    bool IsActive { get; set; }
}

public abstract class AuditableModelBase : IIdentifiable, IAuditable
{
    public long Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }
}

public abstract class DeletableModelBase : IIdentifiable, IDeletable
{
    public long Id { get; set; }
    public bool IsDeleted { get; set; } = false;
}

public abstract class ActivatableModelBase : IIdentifiable, IActivatable
{
    public long Id { get; set; }
    public bool IsActive { get; set; } = true;
}

public abstract class AuditableActivatableModelBase : IIdentifiable, IAuditable, IActivatable
{
    public long Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsActive { get; set; } = true;
}

public abstract class AuditableDeletableModelBase : IIdentifiable, IAuditable, IDeletable
{
    public long Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; } = false;
}

public abstract class FullModelBase : IIdentifiable, IAuditable, IDeletable, IActivatable
{
    public long Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
}