﻿using System;

namespace AzzyBot.Data.Entities;

public sealed class AzuraCastStationChecksEntity
{
    /// <summary>
    /// The database id.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The state of the check if files have been changed.
    /// </summary>
    public bool FileChanges { get; set; }

    /// <summary>
    /// The <see cref="DateTime"/> of the last file changes check.
    /// </summary>
    /// <remarks>
    /// Always use <see cref="DateTime.UtcNow"/> to set this value.
    /// </remarks>
    public DateTime LastFileChangesCheck { get; set; }

    /// <summary>
    /// The database id of the parenting <see cref="AzuraCastStationEntity"/> database item.
    /// </summary>
    public int StationId { get; set; }

    /// <summary>
    /// The parenting <see cref="AzuraCastStationEntity"/> database item.
    /// </summary>
    public AzuraCastStationEntity Station { get; set; } = null!;
}
