using System;
using FrameworkBase.Exceptions;

namespace OlapFormsFramework.Windows.Forms.Grid.Scatter
{
	public class EventArgsSelectionChanged: EventArgs
	{
		private readonly bool _GroupSelectionChanged;
		private readonly string _GroupID;
		private readonly bool _ItemSelectionChanged;
		private readonly int _ItemID;
		private readonly bool _MultipleChanges;

		public EventArgsSelectionChanged(string aGroupID): this(false, true, false, aGroupID, -1){}
		public EventArgsSelectionChanged(int aItemID): this(false, false, true, null, aItemID){}
		public EventArgsSelectionChanged(bool aMultipleChanges, bool aGroupSelectionChanged, bool aItemSelectionChanged): this(aMultipleChanges, aGroupSelectionChanged, aItemSelectionChanged, null, -1){}
		/// <summary>
		/// ������� ����� ��������� �����. 
		/// � ���� ��������� �� ���������, ���. ������ Exception.
		/// </summary>
		/// <param name="aMultipleChanges"><c>True</c> ���� ���������� ���� � ���� � ����������:
		/// * ������ �������� ��� ������ ����
		/// * ������ �������� ��� ������ ��������
		/// * ������ �������� ���� � ��� ���� ����� � ������ �������� ���������
		/// </param>
		/// <param name="aGroupSelectionChanged">����� �� �������� ������ ��� ����</param>
		/// <param name="aItemSelectionChanged">����� �� �������� ������ ��� ��������</param>
		/// <param name="aGroupID">ID ����� ��� ��� �������� ������</param>
		/// <param name="aItemID">ID �������� ��� ����� �������� ������</param>
		/// <exception cref="ArgumentException">��� �������� �� ��� ������������ ������ ������������ ��� ������:
		/// * If MultipleChanges setted then GroupID must be null and ItemID must be -1
		/// * If MultipleChanges not set then GroupSelectionChanged and ItemSelectionChanged can not be set at once
		/// * If GroupID specified then GroupSelectionChanged must be set
		/// * If ItemID specified then ItemSelectionChanged must be set
		/// </exception>
		public EventArgsSelectionChanged(bool aMultipleChanges, bool aGroupSelectionChanged, bool aItemSelectionChanged, string aGroupID, int aItemID): base()
		{
			_MultipleChanges = aMultipleChanges;
			_GroupSelectionChanged = aGroupSelectionChanged;
			_ItemSelectionChanged = aItemSelectionChanged;
			_GroupID = aGroupID;
			_ItemID = aItemID;
			if (_MultipleChanges && (_GroupID != null || _ItemID != -1))
				throw new ArgumentException("If MultipleChanges setted then GroupID must be null and ItemID must be -1.");
			if (!_MultipleChanges && _GroupSelectionChanged && _ItemSelectionChanged)
				throw new ArgumentException("If MultipleChanges not set then GroupSelectionChanged and ItemSelectionChanged can not be set at once");
			if (!_GroupSelectionChanged && _GroupID != null)
				throw new ArgumentException("If GroupID specified then GroupSelectionChanged must be set");
			if (!_ItemSelectionChanged && _ItemID != -1)
				throw new ArgumentException("If ItemID specified then ItemSelectionChanged must be set");
		}

		/// <summary>
		/// Returns <c>true</c> if at least one Group was selected or unselected. Otherwise returns <c>false</c>.
		/// </summary>
		public bool GroupSelectionChanged
		{
			get { return _GroupSelectionChanged; }
		}
		/// <summary>
		/// If <c>GroupSelectionChanges</c> and it is not <c>MultipleChanges</c> then returns an ID of group which was selected or unselected. Otherwise returns <c>null</c>.
		/// </summary>
		public string GroupID
		{
			get { return _GroupID; }
		}
		/// <summary>
		/// Returns <c>true</c> if at least one Item was selected or unselected. Otherwise returns <c>false</c>.
		/// </summary>
		public bool ItemSelectionChanged
		{
			get { return _ItemSelectionChanged; }
		}
		/// <summary>
		/// If <c>ItemsSelectionChanges and it is not <c>MultipleChanges</c> then returns an ID of Item which was selected or unselected. Otherwise returns <c>-1</c>.
		/// </summary>
		public int ItemID
		{
			get { return _ItemID; }
		}
		/// <summary>
		/// Returns <c>false</c> if only one Item or Group state was changed. Otherwise returns <c>true</c>.
		/// </summary>
		public bool MultipleChanges
		{
			get { return _MultipleChanges; }
		}
	}
}