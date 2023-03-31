using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudDriveUI.Models;

public class Node<T> where T: IEquatable<T>
{
    public static void Compare(Node<T> node1, Node<T> node2,out Node<T> onlyInNode1,out Node<T> onlyInNode2,out Node<T> commonNode,Func<T?,T?,bool> func)
    {
        onlyInNode1 = new();
        onlyInNode2 = new();
        commonNode = new();

        if(node1.Value?.Equals(node2.Value)??false)
        {
            commonNode.Value = node1.Value;
        }
        else
        {
            onlyInNode1.Value = node1.Value;
            onlyInNode2.Value = node2.Value;
        }


    }
    public T? Value { get; set; }
    public List<Node<T>> Nodes { get; set; } = new();
    public Node<T>? Parent { get; set; }

    /// <summary>
    /// 插入节点
    /// </summary>
    /// <param name="node">带插入节点</param>
    /// <param name="index">插入位置，默认在队尾</param>
    public void Insert(Node<T> node, int index = -1)
    {
        if (index < 0 || index >= Nodes.Count) Nodes.Add(node);
        else Nodes.Insert(index, node);
    }
}

