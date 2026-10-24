using Lab.Intelligence;
namespace LabUi;
internal sealed class AssistantDialog : Form
{
 private readonly IReadOnlyList<KnowledgeEntry> knowledge;
 private readonly RichTextBox answer=new(){Dock=DockStyle.Fill,ReadOnly=true,BorderStyle=BorderStyle.None,BackColor=Color.FromArgb(250,248,241),Font=new("Segoe UI",11),Text="Ask a short question about the current workflow.\n\nExamples: stock · thanh toán · print · order status"};
 public AssistantDialog(string title,IReadOnlyList<KnowledgeEntry> entries)
 {
  knowledge=entries;Text=title;Size=new(580,470);MinimumSize=new(500,400);StartPosition=FormStartPosition.CenterParent;BackColor=Color.White;Font=new("Segoe UI",10);Padding=new(20);
  Controls.Add(answer);var bar=new TableLayoutPanel{Dock=DockStyle.Bottom,Height=62,Padding=new(0,14,0,0),ColumnCount=2,ColumnStyles={new(SizeType.Percent,100),new(SizeType.Absolute,100)}};Controls.Add(bar);
  var input=new TextBox{Dock=DockStyle.Fill,MaxLength=300,PlaceholderText="Ask your question…"};bar.Controls.Add(input);
  var ask=new Button{Text="Ask →",Dock=DockStyle.Fill,Width=100,Height=32,BackColor=Color.FromArgb(36,113,93),ForeColor=Color.White,FlatStyle=FlatStyle.Flat};bar.Controls.Add(ask);
  ask.Click+=async(_,_)=>{var question=input.Text.Trim();if(question.Length==0)return;ask.Enabled=false;try{var reply=await Task.Run(()=>KnowledgeAssistant.Ask(question,knowledge));if(!IsDisposed)answer.Text=reply.Answer;}finally{if(!IsDisposed){ask.Enabled=true;input.Focus();}}};AcceptButton=ask;
  var note=new Label{Dock=DockStyle.Bottom,Height=28,Text="Local AI guide · Answers from the current application knowledge.",ForeColor=Color.DimGray,Font=new("Segoe UI",8)};Controls.Add(note);
 }
}
