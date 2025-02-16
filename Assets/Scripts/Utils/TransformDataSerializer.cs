using UnityEngine;
using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;

namespace Utils
{
    public static class TransformDataSerializer
    {
        private const float POSITION_PRECISION = 100f; 
        private const int BITS_PER_COMPONENT = 16;
        private const float MAX_POSITION_VALUE = 327.67f; 

        public static byte[] SerializeTransformPosition(Vector3 position)
        {
            // Check if position is within quantizable range
            if (IsQuantizable(position))
            {
                byte[] transformData = new byte[6]; // 2 bytes per component
                
                // Quantize position values
                short quantizedX = QuantizePosition(position.x);
                short quantizedY = QuantizePosition(position.y);
                short quantizedZ = QuantizePosition(position.z);
                
                // Pack the quantized values
                Buffer.BlockCopy(BitConverter.GetBytes(quantizedX), 0, transformData, 0, 2);
                Buffer.BlockCopy(BitConverter.GetBytes(quantizedY), 0, transformData, 2, 2);
                Buffer.BlockCopy(BitConverter.GetBytes(quantizedZ), 0, transformData, 4, 2);
                
                return transformData;
            }
            else
            {
                byte[] transformData = new byte[12]; // 4 bytes per component for raw float
                
                // Pack the raw float values
                Buffer.BlockCopy(BitConverter.GetBytes(position.x), 0, transformData, 0, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(position.y), 0, transformData, 4, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(position.z), 0, transformData, 8, 4);
                
                return transformData;
            }
        }

        public static Vector3 DeserializeTransformPosition(byte[] transformData)
        {
            // Check data format based on array size
            if (transformData.Length == 6) // Quantized data
            {
                short quantizedX = BitConverter.ToInt16(transformData, 0);
                short quantizedY = BitConverter.ToInt16(transformData, 2);
                short quantizedZ = BitConverter.ToInt16(transformData, 4);
                
                return new Vector3(
                    DequantizePosition(quantizedX),
                    DequantizePosition(quantizedY),
                    DequantizePosition(quantizedZ)
                );
            }
            else // Raw float data
            {
                float x = BitConverter.ToSingle(transformData, 0);
                float y = BitConverter.ToSingle(transformData, 4);
                float z = BitConverter.ToSingle(transformData, 8);
                
                return new Vector3(x, y, z);
            }
        }

        public static byte[] GetCompressedData(byte[] data)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                {
                    gzipStream.Write(data, 0, data.Length);
                }
                return memoryStream.ToArray();
            }
        }

        public static byte[] GetDecompressedData(byte[] compressedData)
        {
            using (var memoryStream = new MemoryStream(compressedData))
            {
                using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                {
                    using (var resultStream = new MemoryStream())
                    {
                        gzipStream.CopyTo(resultStream);
                        return resultStream.ToArray();
                    }
                }
            }
        }

        private static short QuantizePosition(float value)
        {
            // Scale and round to nearest integer
            return (short)Mathf.Round(value * POSITION_PRECISION);
        }

        private static bool IsQuantizable(Vector3 position)
        {
            return Mathf.Abs(position.x) <= MAX_POSITION_VALUE &&
                   Mathf.Abs(position.y) <= MAX_POSITION_VALUE &&
                   Mathf.Abs(position.z) <= MAX_POSITION_VALUE;
        }

        private static float DequantizePosition(short quantizedValue)
        {
            return quantizedValue / POSITION_PRECISION;
        }

        public static byte[] SerializeSpawnData(string objectType, Vector3 position, int instanceId)
        {
            using (var memoryStream = new MemoryStream())
            using (var writer = new BinaryWriter(memoryStream))
            {
                // Write object type string length and data
                writer.Write(objectType.Length);
                writer.Write(objectType.ToCharArray());

                // Write position data
                byte[] positionData = SerializeTransformPosition(position);
                writer.Write(positionData.Length);
                writer.Write(positionData);

                // Write instance ID
                writer.Write(instanceId);

                return GetCompressedData(memoryStream.ToArray());
            }
        }

        public static (string objectType, Vector3 position, int instanceId) DeserializeSpawnData(byte[] data)
        {
            byte[] decompressedData = GetDecompressedData(data);

            using (var memoryStream = new MemoryStream(decompressedData))
            using (var reader = new BinaryReader(memoryStream))
            {
                // Read object type
                int typeLength = reader.ReadInt32();
                char[] typeChars = reader.ReadChars(typeLength);
                string objectType = new string(typeChars);

                // Read position data
                int positionDataLength = reader.ReadInt32();
                byte[] positionData = reader.ReadBytes(positionDataLength);
                Vector3 position = DeserializeTransformPosition(positionData);

                // Read instance ID
                int instanceId = reader.ReadInt32();

                return (objectType, position, instanceId);
            }
        }

        public static byte[] SerializeCleanupData(string objectType, int instanceId)
        {
            using (var memoryStream = new MemoryStream())
            using (var writer = new BinaryWriter(memoryStream))
            {
                // Write object type string length and data
                writer.Write(objectType.Length);
                writer.Write(objectType.ToCharArray());

                // Write instance ID
                writer.Write(instanceId);

                return GetCompressedData(memoryStream.ToArray());
            }
        }

        public static (string objectType, int instanceId) DeserializeCleanupData(byte[] data)
        {
            byte[] decompressedData = GetDecompressedData(data);

            using (var memoryStream = new MemoryStream(decompressedData))
            using (var reader = new BinaryReader(memoryStream))
            {
                // Read object type
                int typeLength = reader.ReadInt32();
                char[] typeChars = reader.ReadChars(typeLength);
                string objectType = new string(typeChars);

                // Read instance ID
                int instanceId = reader.ReadInt32();

                return (objectType, instanceId);
            }
        }

        public static (byte[],int) SerializeBatchedSpawnData(List<byte[]> spawnDataList)
        {
            using (var memoryStream = new MemoryStream())
            using (var writer = new BinaryWriter(memoryStream))
            {
                // Write the number of spawn data entries
                writer.Write(spawnDataList.Count);

                // Write each spawn data entry
                foreach (var spawnData in spawnDataList)
                {
                    writer.Write(spawnData.Length);
                    writer.Write(spawnData);
                }

                byte[] rawData = memoryStream.ToArray();
                return (GetCompressedData (rawData), rawData.Length);
            }
        }

        public static List<byte[]> DeserializeBatchedSpawnData(byte[] batchedData)
        {
            byte[] decompressedData = GetDecompressedData(batchedData);
            var result = new List<byte[]>();

            using (var memoryStream = new MemoryStream(decompressedData))
            using (var reader = new BinaryReader(memoryStream))
            {
                // Read the number of spawn data entries
                int count = reader.ReadInt32();

                // Read each spawn data entry
                for (int i = 0; i < count; i++)
                {
                    int length = reader.ReadInt32();
                    byte[] spawnData = reader.ReadBytes(length);
                    result.Add(spawnData);
                }
            }

            return result;
        }
    }
}