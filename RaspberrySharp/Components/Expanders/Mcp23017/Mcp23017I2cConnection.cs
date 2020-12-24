#region References

using RaspberrySharp.IO.InterIntegratedCircuit;

#endregion

namespace RaspberrySharp.Components.Expanders.Mcp23017
{
    /// <summary>
    /// Represents a I2C connection to a MCP23017 I/O Expander.
    /// </summary>
    /// <remarks>See <see cref="http://www.adafruit.com/datasheets/mcp23017.pdf" /> for more information.</remarks>
    public class Mcp23017I2cConnection
    {
        #region Fields

        private readonly I2cDeviceConnection connection;

        #endregion

        #region Instance Management

        /// <summary>
        /// Initializes a new instance of the <see cref="Mcp23017I2cConnection" /> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        public Mcp23017I2cConnection(I2cDeviceConnection connection)
        {
            this.connection = connection;
        }

        #endregion

        #region Private Helpers

        private enum Register : byte
        {
            IODIRA = 0x00,
            IODIRB = 0x01,
            IPOLA = 0x02,
            IPOLB = 0x03,
            GPPUA = 0x0c,
            GPPUB = 0x0d,
            GPIOA = 0x12,
            GPIOB = 0x13
        }

        #endregion

        #region Methods

        public int DeviceAddress => connection.DeviceAddress;

        /// <summary>
        /// Sets the direction.
        /// </summary>
        /// <param name="pin">The pin.</param>
        /// <param name="direction">The direction.</param>
        public void SetDirection(Mcp23017Pin pin, Mcp23017PinDirection direction)
        {
            var register = ((int)pin & 0x0100) == 0x0000 ? Register.IODIRA : Register.IODIRB;

            connection.WriteByte((byte)register);
            var directions = connection.ReadByte();

            var bit = (byte)((int)pin & 0xFF);
            var newDirections = direction == Mcp23017PinDirection.Input
                ? directions | bit
                : directions & ~bit;

            connection.Write((byte)register, (byte)newDirections);
        }

        /// <summary>
        /// Sets the polarity.
        /// </summary>
        /// <param name="pin">The pin.</param>
        /// <param name="polarity">The polarity.</param>
        public void SetPolarity(Mcp23017Pin pin, Mcp23017PinPolarity polarity)
        {
            var register = ((int)pin & 0x0100) == 0x0000 ? Register.IPOLA : Register.IPOLB;

            connection.WriteByte((byte)register);
            var polarities = connection.ReadByte();

            var bit = (byte)((int)pin & 0xFF);
            var newPolarities = polarity == Mcp23017PinPolarity.Inverted
                ? polarities | bit
                : polarities & ~bit;

            connection.Write((byte)register, (byte)newPolarities);
        }

        /// <summary>
        /// Sets the resistor.
        /// </summary>
        /// <param name="pin">The pin.</param>
        /// <param name="resistor">The resistor.</param>
        public void SetResistor(Mcp23017Pin pin, Mcp23017PinResistor resistor)
        {
            var register = ((int)pin & 0x0100) == 0x0000 ? Register.GPPUA : Register.GPPUB;

            connection.WriteByte((byte)register);
            var resistors = connection.ReadByte();

            var bit = (byte)((int)pin & 0xFF);
            var newResistors = resistor == Mcp23017PinResistor.PullUp
                ? resistors | bit
                : resistors & ~bit;

            connection.Write((byte)register, (byte)newResistors);
        }

        /// <summary>
        /// Sets the pin status.
        /// </summary>
        /// <param name="pin">The pin.</param>
        /// <param name="enabled">if set to <c>true</c>, pin is enabled.</param>
        public void SetPinStatus(Mcp23017Pin pin, bool enabled)
        {
            var register = ((int)pin & 0x0100) == 0x0000 ? Register.GPIOA : Register.GPIOB;

            connection.WriteByte((byte)register);
            var status = connection.ReadByte();

            var bit = (byte)((int)pin & 0xFF);
            var newStatus = enabled
                ? status | bit
                : status & ~bit;

            connection.Write((byte)register, (byte)newStatus);
        }

        /// <summary>
        /// Gets the pin status.
        /// </summary>
        /// <param name="pin">The pin.</param>
        /// <returns>The pin status.</returns>
        public bool GetPinStatus(Mcp23017Pin pin)
        {
            var register = ((int)pin & 0x0100) == 0x0000 ? Register.GPIOA : Register.GPIOB;

            connection.WriteByte((byte)register);
            var status = connection.ReadByte();

            var bit = (byte)((int)pin & 0xFF);
            return (status & bit) != 0x00;
        }

        /// <summary>
        /// Toogles the specified pin.
        /// </summary>
        /// <param name="pin">The pin.</param>
        public void Toogle(Mcp23017Pin pin)
        {
            var register = ((int)pin & 0x0100) == 0x0000 ? Register.GPIOA : Register.GPIOB;

            connection.WriteByte((byte)register);
            var status = connection.ReadByte();

            var bit = (byte)((int)pin & 0xFF);
            var bitEnabled = (status & bit) != 0x00;
            var newBitEnabled = !bitEnabled;

            var newStatus = newBitEnabled
                ? status | bit
                : status & ~bit;

            connection.Write((byte)register, (byte)newStatus);
        }

        public void SetDirection(Port port, Mcp23017PinDirection direction)
        {
            var register = GetRegisterAddress(Register.IODIRA, port);

            var value = (byte)(direction == Mcp23017PinDirection.Input
                ? 0xFF
                : 0x00);

            connection.Write(register, value);
        }

        public byte GetPort(Port port) =>
            connection.Read(GetRegisterAddress(Register.GPIOA, port));

        public void SetPort(Port port, byte value) =>
            connection.Write(GetRegisterAddress(Register.GPIOA, port), value);

        public byte GetRegister(byte address) =>
            connection.Read(address);

        public void SetRegister(byte address, byte value) =>
            connection.Write(address, value);

        private static byte GetRegisterAddress(Register baseRegister, Port port) =>
            (byte)((byte)baseRegister + (byte)port);

        #endregion
    }

    public enum Port : byte
    {
        A = 0,
        B = 1,
    }
}