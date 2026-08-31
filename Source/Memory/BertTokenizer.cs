using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RimTelligence.Memory
{
    public class BertTokenizer
    {
        private readonly Dictionary<string, int> vocab;
        private const int MaxSeqLen = 128;
        private const string ClsToken = "[CLS]";
        private const string SepToken = "[SEP]";
        private const string UnkToken = "[UNK]";
        private const string PadToken = "[PAD]";

        public BertTokenizer(string vocabPath)
        {
            vocab = new Dictionary<string, int>();
            var lines = File.ReadAllLines(vocabPath);
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
                vocab[lines[lineIndex]] = lineIndex;
        }

        public (long[] inputIds, long[] attentionMask, long[] tokenTypeIds) Tokenize(string text)
        {
            var tokens = new List<string> { ClsToken };
            tokens.AddRange(WordPieceTokenize(BasicTokenize(text)));
            if (tokens.Count > MaxSeqLen - 1) tokens = tokens.Take(MaxSeqLen - 1).ToList();
            tokens.Add(SepToken);

            int len = tokens.Count;
            var inputIds = new long[MaxSeqLen];
            var attentionMask = new long[MaxSeqLen];
            var tokenTypeIds = new long[MaxSeqLen];

            for (int position = 0; position < MaxSeqLen; position++)
            {
                if (position < len)
                {
                    inputIds[position] = vocab.TryGetValue(tokens[position], out int id) ? id : vocab[UnkToken];
                    attentionMask[position] = 1;
                }
                else
                {
                    inputIds[position] = vocab[PadToken];
                    attentionMask[position] = 0;
                }
                tokenTypeIds[position] = 0;
            }

            return (inputIds, attentionMask, tokenTypeIds);
        }

        private List<string> BasicTokenize(string text)
        {
            text = text.ToLowerInvariant();
            var sb = new StringBuilder();
            foreach (char character in text)
            {
                if (char.IsWhiteSpace(character)) sb.Append(' ');
                else if (char.IsPunctuation(character) || char.IsSymbol(character)) { sb.Append(' '); sb.Append(character); sb.Append(' '); }
                else sb.Append(character);
            }
            return sb.ToString().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        private List<string> WordPieceTokenize(List<string> words)
        {
            var output = new List<string>();
            foreach (var word in words)
            {
                if (word.Length > 100) { output.Add(UnkToken); continue; }

                bool isBad = false;
                int start = 0;
                var subTokens = new List<string>();

                while (start < word.Length)
                {
                    int end = word.Length;
                    string curSubstr = null;
                    while (start < end)
                    {
                        string substr = word.Substring(start, end - start);
                        if (start > 0) substr = "##" + substr;
                        if (vocab.ContainsKey(substr)) { curSubstr = substr; break; }
                        end--;
                    }
                    if (curSubstr == null) { isBad = true; break; }
                    subTokens.Add(curSubstr);
                    start = end;
                }

                if (isBad) output.Add(UnkToken);
                else output.AddRange(subTokens);
            }
            return output;
        }
    }
}