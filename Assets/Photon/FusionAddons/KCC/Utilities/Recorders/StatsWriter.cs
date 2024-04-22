using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Fusion.Addons.KCC
{
    public sealed class StatsWriter
    {
        // CONSTANTS

        public static readonly Encoding DefaultEncoding = Encoding.UTF8;
        public static readonly string DefaultSeparator = ",";
        public static readonly string DefaultComment = "#";
        public static readonly string DefaultNewLine = "\n";
        public static readonly string DefaultPrefix = "Stats";
        public static readonly string DefaultExtension = ".csv";
        private readonly byte[] _buffer = new byte[8192];
        private readonly byte[] _comment;

        private readonly Encoding _encoding;
        private readonly byte[] _newLine;
        private readonly byte[] _separator;
        private int _count;
        private FileStream _fileStream;

        // PRIVATE MEMBERS

        private int _size;
        private string[] _values;

        // CONSTRUCTORS

        public StatsWriter()
        {
            _encoding = DefaultEncoding;
            _separator = _encoding.GetBytes(DefaultSeparator);
            _comment = _encoding.GetBytes(DefaultComment);
            _newLine = _encoding.GetBytes(DefaultNewLine);
        }

        public StatsWriter(Encoding encoding, string separator, string comment, string newLine)
        {
            _encoding = encoding;
            _separator = _encoding.GetBytes(separator);
            _comment = _encoding.GetBytes(comment);
            _newLine = _encoding.GetBytes(newLine);
        }

        // PUBLIC MEMBERS

        public bool IsInitialized => _fileStream != null;
        public bool HasValues => _count > 0;

        // PUBLIC METHODS

        public void Initialize(string fileName, string filePath, int size)
        {
            if (size <= 0)
                throw new ArgumentException(nameof(size));
            if (_fileStream != null)
                return;

            fileName = GetFileName(fileName);
            filePath = GetFilePath(filePath);

            _size = size;
            _values = new string[_size];
            _fileStream = File.OpenWrite(Path.Combine(filePath, fileName));
        }

        public void Initialize(string fileName, string filePath, string caption, params string[] headers)
        {
            if (_fileStream != null)
                return;
            if (headers == null || headers.Length == 0)
                throw new ArgumentNullException(nameof(headers));

            fileName = GetFileName(fileName);
            filePath = GetFilePath(filePath);

            for (var i = 0; i < headers.Length; ++i)
                if (string.IsNullOrEmpty(headers[i]))
                    throw new ArgumentNullException($"{nameof(headers)}[{i}]");

            _size = headers.Length;
            _values = new string[_size];
            _fileStream = File.OpenWrite(Path.Combine(filePath, fileName));

            if (string.IsNullOrEmpty(caption) == false)
            {
                _fileStream.Write(_comment, 0, _comment.Length);

                var captionCount = _encoding.GetBytes(caption, 0, caption.Length, _buffer, 0);
                _fileStream.Write(_buffer, 0, captionCount);

                _fileStream.Write(_newLine, 0, _newLine.Length);
            }

            var header = headers[0];
            var headerCount = _encoding.GetBytes(header, 0, header.Length, _buffer, 0);
            _fileStream.Write(_buffer, 0, headerCount);

            for (var i = 1; i < headers.Length; ++i)
            {
                _fileStream.Write(_separator, 0, _separator.Length);

                header = headers[i];
                headerCount = _encoding.GetBytes(header, 0, header.Length, _buffer, 0);
                _fileStream.Write(_buffer, 0, headerCount);
            }

            _fileStream.Write(_newLine, 0, _newLine.Length);
        }

        public void Deinitialize()
        {
            if (_fileStream == null)
                return;

            _size = 0;
            _values = null;

            _fileStream.Flush(true);
            _fileStream.Close();
            _fileStream = null;
        }

        public void Add(string value)
        {
            if (_count < _size)
            {
                _values[_count] = value;
                ++_count;
            }
        }

        public void Add(int value)
        {
            if (_count < _size)
            {
                _values[_count] = value.ToString(CultureInfo.InvariantCulture);
                ++_count;
            }
        }

        public void Add(float value, string format = "F3")
        {
            if (_count < _size)
            {
                _values[_count] = value.ToString(format, CultureInfo.InvariantCulture);
                ++_count;
            }
        }

        public void Override(int index, string value)
        {
            if (index < 0 || index >= _count)
                throw new ArgumentOutOfRangeException(nameof(index));

            _values[index] = value;
        }

        public void Override(int index, int value)
        {
            if (index < 0 || index >= _count)
                throw new ArgumentOutOfRangeException(nameof(index));

            _values[index] = value.ToString(CultureInfo.InvariantCulture);
        }

        public void Override(int index, float value, string format = "F3")
        {
            if (index < 0 || index >= _count)
                throw new ArgumentOutOfRangeException(nameof(index));

            _values[index] = value.ToString(format, CultureInfo.InvariantCulture);
        }

        public void Clear()
        {
            _count = 0;
        }

        public void Write(bool clear = true)
        {
            if (_count <= 0)
                return;
            if (_count != _size)
                throw new ArgumentException($"Expected {_size} values!");

            var value = _values[0];
            var valueCount = _encoding.GetBytes(value, 0, value.Length, _buffer, 0);

            _fileStream.Write(_buffer, 0, valueCount);

            for (var i = 1; i < _count; ++i)
            {
                _fileStream.Write(_separator, 0, _separator.Length);

                value = _values[i];
                valueCount = _encoding.GetBytes(value, 0, value.Length, _buffer, 0);

                _fileStream.Write(_buffer, 0, valueCount);
            }

            _fileStream.Write(_newLine, 0, _newLine.Length);

            if (clear) _count = 0;
        }

        public void WriteValues(params string[] values)
        {
            if (_fileStream == null)
                return;
            if (values.Length != _size)
                throw new ArgumentException($"Expected {_size} values!");

            var value = values[0];
            var valueCount = _encoding.GetBytes(value, 0, value.Length, _buffer, 0);

            _fileStream.Write(_buffer, 0, valueCount);

            for (var i = 1; i < values.Length; ++i)
            {
                _fileStream.Write(_separator, 0, _separator.Length);

                value = values[i];
                valueCount = _encoding.GetBytes(value, 0, value.Length, _buffer, 0);

                _fileStream.Write(_buffer, 0, valueCount);
            }

            _fileStream.Write(_newLine, 0, _newLine.Length);
        }

        public void WriteComment(string comment)
        {
            if (_fileStream == null)
                return;
            if (string.IsNullOrEmpty(comment))
                return;

            _fileStream.Write(_comment, 0, _comment.Length);

            var commentCount = _encoding.GetBytes(comment, 0, comment.Length, _buffer, 0);
            _fileStream.Write(_buffer, 0, commentCount);

            _fileStream.Write(_newLine, 0, _newLine.Length);
        }

        public void Flush()
        {
            if (_fileStream == null)
                return;

            _fileStream.Flush(true);
        }

        public static string GetUniqueFileName(string prefix = default, string extension = default)
        {
            if (string.IsNullOrEmpty(prefix)) prefix = DefaultPrefix;

            if (string.IsNullOrEmpty(extension)) extension = DefaultExtension;

            return $"{prefix}_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_{Guid.NewGuid()}{extension}";
        }

        public static string GetDirectoryPath(MonoBehaviour behaviour)
        {
#if UNITY_EDITOR
            var filePath = AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(behaviour));
            return Path.GetDirectoryName(filePath);
#else
			return default;
#endif
        }

        public static string GetValidFileName(string fileName, string replacement = "_")
        {
            return string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
        }

        // PRIVATE METHODS

        private static string GetFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName) == false)
                return GetValidFileName(fileName);

            return GetUniqueFileName(DefaultPrefix, DefaultExtension);
        }

        private static string GetFilePath(string path)
        {
            if (string.IsNullOrEmpty(path) == false)
                return path;

            if (Application.isEditor || Application.isMobilePlatform == false)
                return Application.dataPath + "/..";
            return Application.persistentDataPath;
        }
    }
}