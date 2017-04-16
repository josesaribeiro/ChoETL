﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoManifoldWriter : IDisposable
    {
        private TextWriter _textWriter;
        private bool _closeStreamOnDispose = false;
        private ChoManifoldRecordWriter _writer = null;
        public event EventHandler<ChoRowsWrittenEventArgs> RowsWritten;

        public ChoManifoldRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoManifoldWriter(string filePath, ChoManifoldRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Configuration = configuration;

            Init();

            _textWriter = new StreamWriter(ChoPath.GetFullPath(filePath), false, Configuration.Encoding, Configuration.BufferSize);
            _closeStreamOnDispose = true;
        }

        public ChoManifoldWriter(TextWriter textWriter, ChoManifoldRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(textWriter, "TextWriter");

            Configuration = configuration;
            Init();

            _textWriter = textWriter;
        }

        public ChoManifoldWriter(Stream inStream, ChoManifoldRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(inStream, "Stream");

            Configuration = configuration;
            Init();
            _textWriter = new StreamWriter(inStream, Configuration.Encoding, Configuration.BufferSize);
            _closeStreamOnDispose = true;
        }

        public void Dispose()
        {
            if (_closeStreamOnDispose)
                _textWriter.Dispose();
        }

        private void Init()
        {
            if (Configuration == null)
                Configuration = new ChoManifoldRecordConfiguration();

            _writer = new ChoManifoldRecordWriter(Configuration);
            _writer.RowsWritten += NotifyRowsWritten;
        }

        public void Write(IEnumerable records)
        {
            foreach (object rec in records)
                _writer.WriteTo(_textWriter, new object[] { rec }).Loop();
        }

        public void Write(object record)
        {
            _writer.WriteTo(_textWriter, new object[] { record }).Loop();
        }

        public static string ToText(IEnumerable records)
        {
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoManifoldWriter(writer))
            {
                parser.Write(records);

                writer.Flush();
                stream.Position = 0;

                return reader.ReadToEnd();
            }
        }

        private void NotifyRowsWritten(object sender, ChoRowsWrittenEventArgs e)
        {
            EventHandler<ChoRowsWrittenEventArgs> rowsWrittenEvent = RowsWritten;
            if (rowsWrittenEvent == null)
                Console.WriteLine(e.RowsWritten.ToString("#,##0") + " records written.");
            else
                rowsWrittenEvent(this, e);
        }

        #region Fluent API

        public ChoManifoldWriter NotifyAfter(long rowsWritten)
        {
            Configuration.NotifyAfter = rowsWritten;
            return this;
        }

        public ChoManifoldWriter WithFirstLineHeader()
        {
            Configuration.FileHeaderConfiguration.HasHeaderRecord = true;
            return this;
        }

        public ChoManifoldWriter WithRecordSelector(Func<string, Type> recordSelector)
        {
            Configuration.RecordSelector = recordSelector;
            return this;
        }

        public ChoManifoldWriter WithRecordSelector(int startIndex, int size, params Type[] recordTypes)
        {
            Configuration.RecordTypeConfiguration.StartIndex = startIndex;
            Configuration.RecordTypeConfiguration.Size = size;

            if (recordTypes != null)
            {
                foreach (var t in recordTypes)
                    Configuration.RecordTypeConfiguration.RegisterType(t);
            }
            return this;
        }

        #endregion Fluent API
    }
}
