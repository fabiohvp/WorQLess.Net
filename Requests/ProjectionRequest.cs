using System;
using WorQLess.Extensions;

namespace WorQLess.Requests
{
    public interface IProjectionRequest : IRequest
    {
        Type Type { get; }
    }

    public class ProjectionRequest : IProjectionRequest
    {
        private Type _Type;
        public virtual string Name { get; set; }
        public virtual object Args { get; set; }


        public virtual Type Type
        {
            get
            {
                if (string.IsNullOrEmpty(Name))
                {
                    return null;
                }

                if (_Type == null)
                {
                    _Type = Reflection.GetTypeof(WQL.ProjectionsTypes, Name);
                }

                return _Type;
            }
        }
    }
}