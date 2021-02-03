using System;
using NServiceBus;

public class CreateOrder : IMessage
{
    public Guid OrderId { get; set; }
    public int Value { get; set; }
    public string ShipTo { get; set; }
}