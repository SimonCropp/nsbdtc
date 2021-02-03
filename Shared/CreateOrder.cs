using System;
using NServiceBus;

public class CreateOrder : ICommand
{
    public Guid OrderId { get; set; }
    public int Value { get; set; }
}