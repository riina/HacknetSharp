using System.Collections.Generic;
using System.Threading;
using HacknetSharp.Server.Common;

namespace HacknetSharp.Server
{
    public class WorldInstance : World
    {
        private readonly AutoResetEvent _waitHandle;
        public List<object> RegistrationSet { get; }
        public List<object> DirtySet { get; }
        public List<object> DeregistrationSet { get; }

        internal WorldInstance()
        {
            _waitHandle = new AutoResetEvent(true);
            RegistrationSet = new List<object>();
            DirtySet = new List<object>();
            DeregistrationSet = new List<object>();
        }

        public void Tick()
        {
            // TODO update
        }

        public override void RegisterModel<T>(Model<T> model)
        {
            RegistrationSet.Add(model);
        }

        public override void RegisterModels<T>(IEnumerable<Model<T>> models)
        {
            RegistrationSet.AddRange(models);
        }

        public override void DirtyModel<T>(Model<T> model)
        {
            DirtySet.Add(model);
        }

        public override void DirtyModels<T>(IEnumerable<Model<T>> models)
        {
            DirtySet.AddRange(models);
        }

        public override void DeregisterModel<T>(Model<T> model)
        {
            DeregistrationSet.Add(model);
        }

        public override void DeregisterModels<T>(IEnumerable<Model<T>> models)
        {
            DeregistrationSet.AddRange(models);
        }
    }
}
