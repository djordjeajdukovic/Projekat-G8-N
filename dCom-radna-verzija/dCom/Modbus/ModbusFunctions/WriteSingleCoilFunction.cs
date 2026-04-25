using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus write coil functions/requests.
    /// </summary>
    public class WriteSingleCoilFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WriteSingleCoilFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
        public WriteSingleCoilFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusWriteCommandParameters));
        }

        /// <inheritdoc />
        public override byte[] PackRequest()
        {
            ModbusWriteCommandParameters p = (ModbusWriteCommandParameters)CommandParameters;

            byte[] request = new byte[12];

            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)p.TransactionId)), 0, request, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)p.ProtocolId)), 0, request, 2, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)p.Length)), 0, request, 4, 2);

            request[6] = p.UnitId;
            request[7] = p.FunctionCode;

            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)p.OutputAddress)), 0, request, 8, 2);

            ushort coilValue = (p.Value == 0) ? (ushort)0x0000 : (ushort)0xFF00;
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)coilValue)), 0, request, 10, 2);

            return request;
        }

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            Dictionary<Tuple<PointType, ushort>, ushort> retVal = new Dictionary<Tuple<PointType, ushort>, ushort>();
            ModbusWriteCommandParameters p = (ModbusWriteCommandParameters)CommandParameters;

            byte functionCode = response[7];
            if ((functionCode & 0x80) != 0)
            {
                HandeException(response[8]);
            }

            short writtenValueRaw = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(response, 10));
            ushort value = (ushort)(writtenValueRaw == unchecked((short)0xFF00) ? 1 : 0);

            retVal.Add(new Tuple<PointType, ushort>(PointType.DIGITAL_OUTPUT, p.OutputAddress), value);
            return retVal;
        }
    }
}