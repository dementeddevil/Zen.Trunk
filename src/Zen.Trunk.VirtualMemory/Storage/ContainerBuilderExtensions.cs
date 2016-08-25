using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Zen.Trunk.Storage.IO;

namespace Zen.Trunk.Storage
{
    public static class ContainerBuilderExtensions
    {
        public static ContainerBuilder WithVirtualBufferFactory(
            this ContainerBuilder builder, int reservationMB = 32, int bufferSize = 8192)
        {
            builder.RegisterType<VirtualBufferFactory>()
                .WithParameter("reservationMB", reservationMB)
                .WithParameter("bufferSize", bufferSize)
                .As<IVirtualBufferFactory>()
                .SingleInstance();
            return builder;
        }

        public static ContainerBuilder WithBufferDeviceFactory(this ContainerBuilder builder)
        {
            builder.RegisterType<BufferDeviceFactory>()
                .As<IBufferDeviceFactory>()
                .SingleInstance();
            return builder;
        }
    }
}
