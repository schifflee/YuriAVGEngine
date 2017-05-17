﻿using System;
using System.Linq;
using System.Windows.Documents;
using System.Text.RegularExpressions;
using MahApps.Metro.Controls;

namespace Yuri.Hemerocallis.Forms
{
    /// <summary>
    /// StatisticsWindow.xaml 的交互逻辑
    /// </summary>
    public partial class StatisticsWindow : MetroWindow
    {
        /// <summary>
        /// 构造器
        /// </summary>
        public StatisticsWindow()
        {
            InitializeComponent();
            var curPage = StatisticsWindow.core.mainWndRef.CurrentActivePage;
            var orgText = curPage.GetText();
            int L2_WordCount = (int)curPage.WordCount;
            int L3_AllCount = orgText.Count(t => t != '\r' && t != '\n' && t != '\t');
            int L4_Paragraph = curPage.RichTextBox_FlowDocument.Blocks.Count(t => t is Paragraph);
            (int L5_Chinese, int L7_Japanese, int L8_Korea) = this.GetChracterLength(orgText);
            int L6_EngWord = this.GetEnglishLength(orgText);
            int L9_Symbols = orgText.Count(Char.IsPunctuation);
            int L1_All = L5_Chinese + L6_EngWord;
            this.TextBlock_Count.Text = String.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}{9}", Environment.NewLine,
                L1_All, L2_WordCount, L3_AllCount, L4_Paragraph, L5_Chinese, L6_EngWord, L7_Japanese, L8_Korea, L9_Symbols);
        }

        /// <summary>
        /// 计算字符串区域字形的计数
        /// </summary>
        /// <param name="str">要考察的串</param>
        /// <returns>串的区域字形字数</returns>
        public (int ch, int jp, int kr) GetChracterLength(string str)
        {
            int ChCount = 0;
            int JpCount = 0;
            int KrCount = 0;
            foreach (var cc in str)
            {
                if (cc >= 0x4E00 && cc <= 0x9FA5)
                {
                    ChCount++;
                }
                else if (cc >= 0x3040 && cc <= 0x309F || cc >= 0x30A0 && cc <= 0x30FF)
                {
                    JpCount++;
                }
                else if (cc >= 0xAC00 && cc <= 0xD7AF)
                {
                    KrCount++;
                }
            }
            return (ChCount, JpCount, KrCount);
        }

        /// <summary>
        /// 计算字符串中的英文单词数
        /// </summary>
        /// <param name="str">要考察的串</param>
        /// <returns>串中的英文单词数</returns>
        public int GetEnglishLength(string str)
        {
            return string.IsNullOrEmpty(str) ? 0 : Regex.Matches(str.Replace("\n", " ").Replace("\r", " "), @"[A-Za-z][A-Za-z'\-.]*").Count;
        }

        /// <summary>
        /// 后台引用
        /// </summary>
        private static readonly Controller core = Controller.GetInstance();
    }
}
