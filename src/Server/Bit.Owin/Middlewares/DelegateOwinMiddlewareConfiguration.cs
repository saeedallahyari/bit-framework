﻿using System;
using Bit.Owin.Contracts;
using Owin;

namespace Bit.Owin.Middlewares
{
    public class DelegateOwinMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        protected DelegateOwinMiddlewareConfiguration()
        {

        }

        private readonly Action<IAppBuilder> _owinAppCustomizer;

        public DelegateOwinMiddlewareConfiguration(Action<IAppBuilder> owinAppCustomizer)
        {
            if (owinAppCustomizer == null)
                throw new ArgumentNullException(nameof(owinAppCustomizer));

            _owinAppCustomizer = owinAppCustomizer;
        }

        public virtual void Configure(IAppBuilder owinApp)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            _owinAppCustomizer(owinApp);
        }
    }
}
