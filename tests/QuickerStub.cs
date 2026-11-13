// Quicker.Public.IStepContext 的测试替身（版本 0.01）
using System;
using System.Collections.Generic;

namespace Quicker.Public
{
    public interface IStepContext
    {
        object GetVarValue(string varName);
        void SetVarValue(string varName, object value);
    }
}

public sealed class TestContext : Quicker.Public.IStepContext
{
    private readonly Dictionary<string, object> values = new Dictionary<string, object>();

    public object GetVarValue(string varName)
    {
        object value;
        if (values.TryGetValue(varName, out value))
        {
            return value;
        }
        return null;
    }

    public void SetVarValue(string varName, object value)
    {
        values[varName] = value;
    }

    public void Set(string varName, object value)
    {
        values[varName] = value;
    }

    public object Get(string varName)
    {
        object value;
        if (values.TryGetValue(varName, out value))
        {
            return value;
        }
        return null;
    }
}
