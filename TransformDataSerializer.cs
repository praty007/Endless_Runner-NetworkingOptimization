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

        #region Position Serialization
        public static byte[] SerializeTransformPosition(Vector3 position)
        {
            if (IsQuantizable(position))
            {
                return SerializeQuantizedPosition(position);
            }
            return SerializeRawPosition(position);
        }

        private static byte[] SerializeQuantizedPosition(Vector3 position)
        {
            byte[] transformData = new byte[6];
            short[] quantizedValues = new short[]
            {
                QuantizePosition(position.x),
                QuantizePosition(position.y),
                QuantizePosition(position.z)
            };

            for (int i = 0; i < 3; i++)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(quantizedValues[i]), 0, transformData, i * 2, 2);
            }
            return transformData;
        }

        private static byte[] SerializeRawPosition(Vector3 position)
        {
            byte[] transformData = new byte[12];
            float[] values = new float[] { position.x, position.y, position.z };

            for (int i = 0; i < 3; i++)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(values[i]), 0, transformData, i * 4, 4);
            }
            return transformData;
        }

        public static Vector3 DeserializeTransformPosition(byte[] transformData)
        {
            return transformData.Length == 6 ? 
                DeserializeQuantizedPosition(transformData) : 
                DeserializeRawPosition(transformData);
        }

        private static Vector3 DeserializeQuantizedPosition(byte[] transformData)
        {
            short[] quantizedValues = new short[3];
            for (int i = 0; i < 3; i++)
            {
                quantizedValues[i] = BitConverter.ToInt16(transformData, i * 2);
            }

            return new Vector3(
                DequantizePosition(quantizedValues[0]),
                DequantizePosition(quantizedValues[1]),
                DequantizePosition(quantizedValues[2])
            );
        }

        private static Vector3 DeserializeRawPosition(byte[] transformData)
        {
            float[] values = new float[3];
            for (int i = 0; i < 3; i++)
            {
                values[i] = BitConverter.ToSingle(transformData, i * 4);
            }

            return new Vector3(values[0], values[1], values[2]);
        }
        #endregion

        #region Compression Utilities
        public static byte[] GetCompressedData(byte[] data)
        {
            using (var memoryStream = new MemoryStream())
            using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
            {
                gzipStream.Write(data, 0, data.Length);
                return memoryStream.ToArray();
            }
        }

        public static byte[] GetDecompressedData(byte[] compressedData)
        {
            using (var memoryStream = new MemoryStream(compressedData))
            using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                byte[] buffer = new byte[4096];
                int count;
                while ((count = gzipStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    resultStream.Write(buffer, 0, count);
                }
                return resultStream.ToArray();
            }
        }
        #endregion

        #region Position Utilities
        private static short QuantizePosition(float value)
        {
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
        #endregion

        #region Binary Serialization Utilities
        private static void WriteString(BinaryWriter writer, string value)
        {
            Debug.Log(value.Length + "   " + ushort.MaxValue);
            writer.Write((ushort)value.Length);
            writer.Write(value.ToCharArray());
        }

        private static string ReadString(BinaryReader reader)
        {
            ushort length = reader.ReadUInt16();
            return new string(reader.ReadChars(length));
        }

        private static void WriteTransformPosition(BinaryWriter writer, Vector3 position)
        {
            byte[] positionData = SerializeTransformPosition(position);
            writer.Write((byte)positionData.Length);
            writer.Write(positionData);
        }

        private static Vector3 ReadTransformPosition(BinaryReader reader)
        {
            byte positionDataLength = reader.ReadByte();
            return DeserializeTransformPosition(reader.ReadBytes(positionDataLength));
        }
        #endregion

        public enum ObjectType
        {
            Obstacle,
            Coin
        }

        #region Spawn Data Serialization
        public static byte[] SerializeSpawnData(ObjectType objectType, Vector3 position, int instanceId)
        {
            using (var memoryStream = new MemoryStream())
            using (var writer = new BinaryWriter(memoryStream))
            {
                WriteString(writer, objectType.ToString());
                WriteTransformPosition(writer, position);
                writer.Write(instanceId);
                return GetCompressedData(memoryStream.ToArray());
            }
        }

        public static (ObjectType objectType, Vector3 position, int instanceId) DeserializeSpawnData(byte[] data)
        {
            using (var memoryStream = new MemoryStream(GetDecompressedData(data)))
            using (var reader = new BinaryReader(memoryStream))
            {
                return (
                    Enum.Parse<ObjectType>(ReadString(reader)),
                    ReadTransformPosition(reader),
                    reader.ReadInt32()
                );
            }
        }
        #endregion

        #region Cleanup Data Serialization
        public static byte[] SerializeCleanupData(ObjectType objectType, int instanceId)
        {
            using (var memoryStream = new MemoryStream())
            using (var writer = new BinaryWriter(memoryStream))
            {
                WriteString(writer, objectType.ToString());
                writer.Write(instanceId);
                return GetCompressedData(memoryStream.ToArray());
            }
        }

        public static (ObjectType objectType, int instanceId) DeserializeCleanupData(byte[] data)
        {
            using (var memoryStream = new MemoryStream(GetDecompressedData(data)))
            using (var reader = new BinaryReader(memoryStream))
            {
                return (
                    Enum.Parse<ObjectType>(ReadString(reader)),
                    reader.ReadInt32()
                );
            }
        }
        #endregion

        #region Collision Data Serialization
        public static byte[] SerializeCollisionData(ObjectType objectType, int instanceId, int value)
        {
            using (var memoryStream = new MemoryStream())
            using (var writer = new BinaryWriter(memoryStream))
            {
                WriteString(writer, objectType.ToString());
                writer.Write(instanceId);
                writer.Write(value);
                return GetCompressedData(memoryStream.ToArray());
            }
        }

        public static (ObjectType objectType, int instanceId, int value) DeserializeCollisionData(byte[] data)
        {
            using (var memoryStream = new MemoryStream(GetDecompressedData(data)))
            using (var reader = new BinaryReader(memoryStream))
            {
                return (
                    Enum.Parse<ObjectType>(ReadString(reader)),
                    reader.ReadInt32(),
                    reader.ReadInt32()
                );
            }
        }
        #endregion
    }
}