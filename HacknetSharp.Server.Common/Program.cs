using System;
using System.Collections.Generic;

namespace HacknetSharp.Server.Common
{
    public abstract class Program
    {
        public abstract IEnumerator<Token?> Invoke(System system);

        public static DelayToken Delay(float delay) => new DelayToken(delay);
        public static ConditionToken Condition(Func<bool> condition) => new ConditionToken(condition);

        public abstract class Token
        {
        }

        public class DelayToken : Token
        {
            public float Delay;

            public DelayToken(float delay)
            {
                Delay = delay;
            }
        }

        public class ConditionToken : Token
        {
            public Func<bool> Condition;

            public ConditionToken(Func<bool> condition)
            {
                Condition = condition;
            }
        }
    }
}
