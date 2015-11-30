﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LyyneheymCore.SlyviaCore;

namespace LyyneheymCore.SlyviaPile
{
    /// <summary>
    /// 语义分析器：将语法树翻译为运行时环境能够解析的中间代码
    /// </summary>
    public sealed class Pile
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public Pile()
        {
            this.lexer = new Lexer();
            this.parser = new Parser();
            this.symboler = SymbolTable.getInstance();
        }

        /// <summary>
        /// 复位语义分析器
        /// </summary>
        public void Reset()
        {
            this.parseTree = null;
        }

        /// <summary>
        /// 进行一趟用户脚本编译，并把所有语句子树规约到一个共同的根节点上，
        /// 并返回语义分析、流程逻辑处理和代码优化后的动作序列向量
        /// </summary>
        /// <param name="sourceCodeItem">以行分割的源代码字符串向量</param>
        public void StartDash(List<string> sourceCodeItem)
        {
            // 变量初期化
            this.parseTree = new SyntaxTreeNode(SyntaxType.case_kotori);
            this.parseTree.nodeName = "myKotori_Root";
            this.symboler.AddTable(this.parseTree);
            this.parser.iBlockStack.Push(this.parseTree);
            foreach (string s in sourceCodeItem)
            {
                // 词法分析
                this.lexer.Init(s);
                List<Token> tokenStream = this.lexer.Analyse();
                // 语法分析
                if (tokenStream.Count > 0)
                {
                    this.parser.SetTokenStream(tokenStream);
                    this.parser.Parse();
                }
            }
            string ggs = this.parseTree.ToString();
            Console.WriteLine(ggs);
        }

        /// <summary>
        /// 启动语义分析器，返回动作语法树对应的序列
        /// </summary>
        /// <param name="root">语法树根节点</param>
        /// <returns>动作序列的头部</returns>
        private SceneAction Semanticer(SyntaxTreeNode root)
        {
            SceneAction resSa = new SceneAction();
            this.Mise(this.parseTree, resSa, 0);
            return resSa;
        }

        /// <summary>
        /// 语法树遍历
        /// </summary>
        /// <param name="mynode">递归节点</param>
        /// <param name="curSa">当前动作序列</param>
        /// <param name="flag">触发器类型</param>
        private void Mise(SyntaxTreeNode mynode, SceneAction curSa, int flag)
        {
            // 自顶向下递归遍历语法树
            switch (mynode.nodeSyntaxType)
            {
                case SyntaxType.Unknown:
                case SyntaxType.case_wexpr:
                case SyntaxType.case_disjunct:
                    break;
                case SyntaxType.case_kotori:
                    // 如果是总的根节点
                    if (curSa == null)
                    {
                        curSa = new SceneAction();
                    }
                    if (mynode.children == null)
                    {
                        break;
                    }
                    List<SceneAction> kotoriTrueList = new List<SceneAction>();
                    curSa.trueRouting = kotoriTrueList;
                    // 递归遍历
                    foreach (SyntaxTreeNode child in mynode.children)
                    {
                        SceneAction sa = new SceneAction();
                        sa.aType = (SActionType)Enum.Parse(typeof(SActionType), "act_" + child.nodeSyntaxType.ToString().Replace("synr_", ""));
                        // 跳过增广文法节点，拷贝参数字典
                        if (child.nodeSyntaxType.ToString().StartsWith("synr_")
                            && child.paramDict != null)
                        {
                            foreach (KeyValuePair<string, SyntaxTreeNode> kvp in mynode.paramDict)
                            {
                                sa.argsDict.Add(kvp.Key, kvp.Value.children[0]);
                            }
                        }
                        // 接下来递归这些孩子，加到真分支去
                        kotoriTrueList.Add(sa);
                        this.Mise(child, sa, flag);
                    }
                    // 处理序列关系
                    for (int i = 0; i < kotoriTrueList.Count - 1; i++)
                    {
                        kotoriTrueList[i].next = kotoriTrueList[i + 1];
                    }
                    break;
                case SyntaxType.synr_if:
                    // 处理条件指针
                    curSa.condPointer = mynode.paramDict["cond"];
                    // 处理真分支
                    curSa.trueRouting = new List<SceneAction>();
                    if (mynode.children[0] == null)
                    {
                        break;
                    }
                    SceneAction saIfTrue = new SceneAction();
                    this.Mise(mynode.children[0], saIfTrue, flag);
                    // 处理假分支
                    curSa.falseRouting = new List<SceneAction>();
                    if (mynode.children[1] == null)
                    {
                        break;
                    }
                    SceneAction saIfFalse = new SceneAction();
                    this.Mise(mynode.children[1], saIfFalse, flag);
                    break;
                case SyntaxType.synr_for:
                    // 处理条件指针
                    if (mynode.paramDict.ContainsKey("cond"))
                    {
                        curSa.condPointer = mynode.paramDict["cond"];
                    }
                    // 处理真分支
                    curSa.trueRouting = new List<SceneAction>();
                    if (mynode.children[0] == null)
                    {
                        break;
                    }
                    SceneAction saForTrue = new SceneAction();
                    this.Mise(mynode.children[0], saForTrue, flag);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 抽象语法树求表达式值
        /// </summary>
        /// <param name="mynode">递归节点</param>
        /// <param name="myproxy">代理器</param>
        /// <returns></returns>
        public static bool AST(SyntaxTreeNode mynode, SceneAction myproxy)
        {
            switch (mynode.nodeSyntaxType)
            {
                case SyntaxType.case_wexpr:
                    if (mynode.candidateFunction.GetCFType() == CFunctionType.deri___wexpr__wmulti__wexpr_pi_45)
                    {
                        AST(mynode.children[0], myproxy); // 因式
                        AST(mynode.children[1], myproxy); // 加项
                        if (mynode.children[0].aTag.GetType() == typeof(double) &&
                            mynode.children[1].aTag.GetType() == typeof(double))
                        {
                            mynode.aTag = (double)mynode.children[0].aTag + (double)mynode.children[1].aTag;
                        }
                        else
                        {
                            mynode.aTag = (string)mynode.children[0].aTag + (string)mynode.children[1].aTag;
                        }
                    }
                    else
                    {
                        mynode.aTag = 0;
                    }
                    break;
                case SyntaxType.case_wexpr_pi:
                    if (mynode.candidateFunction.GetCFType() == CFunctionType.deri___wexpr_pi__wplus__wexpr_pi_72)
                    {
                        AST(mynode.children[0], myproxy); // 加项
                        AST(mynode.children[1], myproxy); // 加项闭包
                        if (mynode.children[0].aTag.GetType() == typeof(double) &&
                            mynode.children[1].aTag.GetType() == typeof(double))
                        {
                            mynode.aTag = (double)mynode.children[0].aTag + (double)mynode.children[1].aTag;
                        }
                        else
                        {
                            mynode.aTag = (string)mynode.children[0].aTag + (string)mynode.children[1].aTag;
                        }
                    }
                    break;
                case SyntaxType.case_wplus:
                    if (mynode.candidateFunction.GetCFType() == CFunctionType.deri___wplus__plus_wmulti_46)
                    {
                        AST(mynode.children[1], myproxy);
                        mynode.aTag = mynode.children[1].aTag; // 加法
                    }
                    else if (mynode.candidateFunction.GetCFType() == CFunctionType.deri___wplus__minus_wmulti_47)
                    {
                        AST(mynode.children[1], myproxy);
                        // 减法
                        if (mynode.children[1].aTag.GetType() == typeof(double))
                        {
                            mynode.aTag = (-1.0) * (double)mynode.children[1].aTag;
                        }
                        else
                        {
                            mynode.aTag = "-" + (string)mynode.children[1].aTag;
                        }
                    }
                    else
                    {
                        mynode.aTag = 0;
                    }
                    break;
                case SyntaxType.case_wmulti:
                    AST(mynode.children[0], myproxy); // 乘项
                    AST(mynode.children[1], myproxy); // 乘项闭包
                    if (mynode.children[0].aTag.GetType() == typeof(double) &&
                        mynode.children[1].aTag.GetType() == typeof(double))
                    {
                        mynode.aTag = (double)mynode.children[0].aTag * (double)mynode.children[1].aTag;
                    }
                    else
                    {
                        mynode.aTag = (string)mynode.children[0].aTag + "*" + (string)mynode.children[1].aTag;
                    }
                    break;
                case SyntaxType.case_wmultiOpt:
                    if (mynode.candidateFunction.GetCFType() == CFunctionType.deri___wmultiOpt__multi_wunit__wmultiOpt_50)
                    {
                        AST(mynode.children[1], myproxy); // 乘项
                        AST(mynode.children[2], myproxy); // 乘项闭包
                        // 乘法
                        if (mynode.children[1].aTag.GetType() == typeof(double) &&
                        mynode.children[2].aTag.GetType() == typeof(double))
                        {
                            mynode.aTag = (double)mynode.children[1].aTag * (double)mynode.children[2].aTag;
                        }
                        else
                        {
                            mynode.aTag = (string)mynode.children[1].aTag + "*" + (string)mynode.children[2].aTag;
                        }
                    }
                    else if (mynode.candidateFunction.GetCFType() == CFunctionType.deri___wmultiOpt__div_wunit__wmultiOpt_51)
                    {
                        AST(mynode.children[1], myproxy); // 乘项
                        AST(mynode.children[2], myproxy); // 乘项闭包

                        if (mynode.children[1].aTag.GetType() == typeof(double) &&
                        mynode.children[2].aTag.GetType() == typeof(double))
                        {
                            if ((double)mynode.children[1].aTag * (double)mynode.children[2].aTag == 0)
                            {
                                mynode.aTag = 0.0;
                                // 除零错误
                            }
                            else
                            {
                                mynode.aTag = 1.0f / (double)mynode.children[1].aTag * (double)mynode.children[2].aTag;
                            }

                            mynode.aTag = (double)mynode.children[1].aTag * (double)mynode.children[2].aTag;
                        }
                        else
                        {
                            mynode.aTag = (string)mynode.children[1].aTag + "/" + (string)mynode.children[2].aTag;
                        }
                    }
                    else
                    {
                        mynode.aTag = 1.0f;
                    }
                    break;
                case SyntaxType.case_wunit:
                    if (mynode.candidateFunction.GetCFType() == CFunctionType.deri___wunit__number_53)
                    {
                        mynode.aTag = Convert.ToDouble(mynode.children[0].nodeValue);
                    }
                    else if (mynode.candidateFunction.GetCFType() == CFunctionType.deri___wunit__minus_wunit_55)
                    {
                        AST(mynode.children[1], myproxy);
                        if (mynode.children[1].aTag.GetType() == typeof(double))
                        {
                            mynode.aTag = (-1) * (double)mynode.children[1].aTag;
                        }
                        else
                        {
                            mynode.aTag = "-" + (string)mynode.children[1].aTag;
                        }

                    }
                    else if (mynode.candidateFunction.GetCFType() == CFunctionType.deri___wunit__plus_wunit_56)
                    {
                        AST(mynode.children[1], myproxy);
                        mynode.aTag = mynode.children[1].aTag;
                    }
                    else if (mynode.candidateFunction.GetCFType() == CFunctionType.deri___wunit__brucket_disjunct_57)
                    {
                        AST(mynode.children[1], myproxy);
                        mynode.aTag = mynode.children[1].aTag;
                    }
                    else if (mynode.candidateFunction.GetCFType() == CFunctionType.deri___wunit__iden_54)
                    {
                        //mynode.aTag = myexec->Reference(mynode.children[0].nodeValue); // 查参数字典
                        // 这里需要绑定运行时环境的变量
                    }
                    break;
                case SyntaxType.case_disjunct:
                    AST(mynode.children[0], myproxy); // 合取项
                    AST(mynode.children[1], myproxy); // 析取闭包
                    mynode.aTag = (bool)mynode.children[0].aTag || (bool)mynode.children[1].aTag;
                    return (bool)mynode.aTag;
                case SyntaxType.case_disjunct_pi:
                    if (mynode.candidateFunction.GetCFType() == CFunctionType.deri___disjunct_pi__conjunct__disjunct_pi_36)
                    {
                        AST(mynode.children[1], myproxy); // 合取项
                        AST(mynode.children[2], myproxy); // 析取闭包
                        mynode.aTag = (bool)mynode.children[1].aTag || (bool)mynode.children[2].aTag;
                    }
                    else
                    {
                        mynode.aTag = false; // 析取false不影响结果
                    }
                    break;
                case SyntaxType.case_conjunct:
                    AST(mynode.children[0], myproxy); // 布尔项
                    AST(mynode.children[1], myproxy); // 合取闭包
                    mynode.aTag = (bool)mynode.children[0].aTag && (bool)mynode.children[1].aTag;
                    break;
                case SyntaxType.case_conjunct_pi:
                    if (mynode.candidateFunction.GetCFType() == CFunctionType.deri___conjunct_pi__bool__conjunct_pi_39)
                    {
                        AST(mynode.children[1], myproxy); // 布尔项
                        AST(mynode.children[2], myproxy); // 合取闭包
                        mynode.aTag = (bool)mynode.children[1].aTag && (bool)mynode.children[2].aTag;
                    }
                    else
                    {
                        mynode.aTag = true; // 合取true不影响结果
                    }
                    break;
                case SyntaxType.case_bool:
                    if (mynode.candidateFunction.GetCFType() == CFunctionType.deri___bool__not_bool_42)
                    {
                        AST(mynode.children[1], myproxy); // 非项
                        mynode.aTag = ((bool)mynode.children[1].aTag) == false;
                    }
                    else
                    {
                        AST(mynode.children[0], myproxy); // 表达式
                        mynode.aTag = (bool)mynode.children[0].aTag;
                    }
                    break;
                case SyntaxType.case_comp:
                    if (mynode.children[1].candidateFunction.GetCFType() == CFunctionType.deri___rop__epsilon_80)
                    {
                        AST(mynode.children[0], myproxy); // 左边
                        mynode.aTag = mynode.children[0].aTag;
                    }
                    else
                    {
                        string optype = mynode.children[1].nodeValue; // 运算符
                        AST(mynode.children[0], myproxy); // 左边
                        AST(mynode.children[2], myproxy); // 右边
                        mynode.aTag = false;
                        if (mynode.children[0].aTag.GetType() != typeof(double) && 
                            mynode.children[2].aTag.GetType() != typeof(double))
                        {
                            if (optype == "<>")
                            {
                                mynode.aTag = string.Compare((string)mynode.children[0].aTag, (string)mynode.children[2].aTag) != 0;
                            }
                            else if (optype == "==")
                            {
                                mynode.aTag = string.Compare((string)mynode.children[0].aTag, (string)mynode.children[2].aTag) == 0;
                            }
                            else if (optype == ">")
                            {
                                mynode.aTag = string.Compare((string)mynode.children[0].aTag, (string)mynode.children[2].aTag) > 0;
                            }
                            else if (optype == "<")
                            {
                                mynode.aTag = string.Compare((string)mynode.children[0].aTag, (string)mynode.children[2].aTag) < 0;
                            }
                            else if (optype == ">=")
                            {
                                mynode.aTag = string.Compare((string)mynode.children[0].aTag, (string)mynode.children[2].aTag) >= 0;
                            }
                            else if (optype == "<=")
                            {
                                mynode.aTag = string.Compare((string)mynode.children[0].aTag, (string)mynode.children[2].aTag) <= 0;
                            }
                        }
                        else
                        {
                            if (optype == "<>")
                            {
                                mynode.aTag = (double)mynode.children[0].aTag != (double)mynode.children[2].aTag;
                            }
                            else if (optype == "==")
                            {
                                mynode.aTag = (double)mynode.children[0].aTag == (double)mynode.children[2].aTag;
                            }
                            else if (optype == ">")
                            {
                                mynode.aTag = (double)mynode.children[0].aTag > (double)mynode.children[2].aTag;
                            }
                            else if (optype == "<")
                            {
                                mynode.aTag = (double)mynode.children[0].aTag < (double)mynode.children[2].aTag;
                            }
                            else if (optype == ">=")
                            {
                                mynode.aTag = (double)mynode.children[0].aTag >= (double)mynode.children[2].aTag;
                            }
                            else if (optype == "<=")
                            {
                                mynode.aTag = (double)mynode.children[0].aTag <= (double)mynode.children[2].aTag;
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
        }


        private Lexer lexer = null;
        private Parser parser = null;
        private SymbolTable symboler = null;
        private SceneAction rootSa = null;
        private SyntaxTreeNode parseTree = null;

    }
}