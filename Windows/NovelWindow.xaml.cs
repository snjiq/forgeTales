using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using ForgeTales.Classes;
using GalaSoft.MvvmLight.Command;

namespace ForgeTales.Windows
{
    public partial class NovelWindow : Window
    {
        public NovelWindow(List<DialogueLine> dialogueLines, string title)
        {
            InitializeComponent();
            DataContext = new NovelViewModel(dialogueLines, title);
        }
    }

    public class NovelViewModel : INotifyPropertyChanged
    {
        private readonly List<DialogueLine> _dialogue;
        private int _currentIndex;

        public string Title { get; }
        public string CurrentSpeaker => _dialogue[_currentIndex].Character;
        public string CurrentText => _dialogue[_currentIndex].Text;

        public RelayCommand NextCommand { get; }
        public RelayCommand BackCommand { get; }

        public NovelViewModel(List<DialogueLine> dialogue, string title)
        {
            _dialogue = dialogue;
            Title = title;

            NextCommand = new RelayCommand(
                execute: NextLine,
                canExecute: () => _currentIndex < _dialogue.Count - 1);

            BackCommand = new RelayCommand(
                execute: PrevLine,
                canExecute: () => _currentIndex > 0);
        }

        private void NextLine()
        {
            _currentIndex++;
            OnPropertyChanged(nameof(CurrentSpeaker));
            OnPropertyChanged(nameof(CurrentText));
            NextCommand.RaiseCanExecuteChanged();
            BackCommand.RaiseCanExecuteChanged();
        }

        private void PrevLine()
        {
            _currentIndex--;
            OnPropertyChanged(nameof(CurrentSpeaker));
            OnPropertyChanged(nameof(CurrentText));
            NextCommand.RaiseCanExecuteChanged();
            BackCommand.RaiseCanExecuteChanged();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}