﻿using HomeEdu.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

public class Speaker : IEntity
{
    public int Id { get; set; }
    public string? ImagePath { get; set; }
    public string Name { get; set; } = null!;
    public string Surname { get; set; } = null!;
    public string? passCode { get; set; } 
    public string? Position { get; set; }
    // Many-to-one relationship with EventDetail
    public int? EventDetailId { get; set; }
    public EventDetail? EventDetail { get; set; }
}
