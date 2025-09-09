namespace DuncanCraft.Lighting
{
    public class NibbleArray
    {
        private readonly byte[] _mData;
        private readonly int _mSize;
        
        public NibbleArray(int size)
        {
            _mSize = size;
            _mData = new byte[(size + 1) / 2];
        }

        public byte GetNibble(int index)
        {
            if (index < 0 || index >= _mSize)
                return 0;
                
            int byteIndex = index / 2;
            bool isUpperNibble = (index % 2) == 1;
            
            if (isUpperNibble)
                return (byte)((_mData[byteIndex] >> 4) & 0xF);
            else
                return (byte)(_mData[byteIndex] & 0xF);
        }
        
        public byte GetNibble(int x, int y, int z)
        {
            int index = (x * 64 + y) * 16 + z;
            return GetNibble(index);
        }

        public void SetNibble(int index, byte value)
        {
            if (index < 0 || index >= _mSize || value > 15)
                return;
                
            int byteIndex = index / 2;
            bool isUpperNibble = (index % 2) == 1;
            
            if (isUpperNibble)
            {
                _mData[byteIndex] = (byte)((_mData[byteIndex] & 0x0F) | ((value & 0xF) << 4));
            }
            else
            {
                _mData[byteIndex] = (byte)((_mData[byteIndex] & 0xF0) | (value & 0xF));
            }
        }

        public void SetNibble(int x, int y, int z, byte value)
        {
            int index = (x * 64 + y) * 16 + z;
            SetNibble(index, value);
        }

        public void Fill(byte value)
        {
            if (value > 15) value = 15;
            
            byte fillByte = (byte)((value << 4) | value);
            for (int i = 0; i < _mData.Length; i++)
            {
                _mData[i] = fillByte;
            }
        }

        public void Clear()
        {
            Array.Fill(_mData, (byte)0);
        }
    }
}