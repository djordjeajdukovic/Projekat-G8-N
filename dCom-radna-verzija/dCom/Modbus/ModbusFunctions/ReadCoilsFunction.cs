using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus read coil functions/requests.
    /// </summary>
    public class ReadCoilsFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadCoilsFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
		public ReadCoilsFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusReadCommandParameters));
        }

        /// <inheritdoc/>
        public override byte[] PackRequest()
        {
            ModbusReadCommandParameters p = (ModbusReadCommandParameters)CommandParameters;

            byte[] request = new byte[12];

            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)p.TransactionId)), 0, request, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)p.ProtocolId)), 0, request, 2, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)p.Length)), 0, request, 4, 2);

            request[6] = p.UnitId;
            request[7] = p.FunctionCode;

            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)p.StartAddress)), 0, request, 8, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)p.Quantity)), 0, request, 10, 2);

            return request;
        }

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            Dictionary<Tuple<PointType, ushort>, ushort> retVal = new Dictionary<Tuple<PointType, ushort>, ushort>();
            ModbusReadCommandParameters p = (ModbusReadCommandParameters)CommandParameters;

            byte functionCode = response[7];
            if ((functionCode & 0x80) != 0)
            {
                HandeException(response[8]);
            }

            byte byteCount = response[8];

            for (int i = 0; i < p.Quantity; i++)
            {
                int dataByteIndex = 9 + (i / 8);
                int bitIndex = i % 8;

                if ((dataByteIndex - 9) < byteCount)
                {
                    ushort value = (ushort)((response[dataByteIndex] >> bitIndex) & 0x01);
                    retVal.Add(new Tuple<PointType, ushort>(PointType.DIGITAL_OUTPUT, (ushort)(p.StartAddress + i)), value);
                }
            }

            return retVal;
        }
    }
}