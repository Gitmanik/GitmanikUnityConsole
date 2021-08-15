using System;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class ConsoleCommandAttribute : Attribute
{
    public string command;
    public string description;

    public ConsoleCommandAttribute(string command, string description)
    {
        this.command = command;
        this.description = description;
    }
}
