import React from 'react';
import './CluePanel.css';

export interface ClueTab {
  id: string;
  title: string;
  url: string;
  wordIndex: number;
}

interface CluePanelProps {
  tabs: ClueTab[];
  activeTabId: string | null;
  onTabSelect: (tabId: string) => void;
  onTabClose: (tabId: string) => void;
}

export const CluePanel: React.FC<CluePanelProps> = ({ 
  tabs, 
  activeTabId, 
  onTabSelect, 
  onTabClose 
}) => {
  if (tabs.length === 0) {
    return (
      <div className="clue-panel">
        <div className="clue-panel-empty">
          <div className="empty-icon">🔍</div>
          <h3>Clue Zone</h3>
          <p>Click a clue button to reveal hints here!</p>
        </div>
      </div>
    );
  }

  const activeTab = tabs.find(t => t.id === activeTabId) || tabs[0];

  return (
    <div className="clue-panel">
      <div className="clue-tabs">
        {tabs.map((tab) => (
          <div 
            key={tab.id}
            className={`clue-tab ${tab.id === activeTab.id ? 'active' : ''}`}
            onClick={() => onTabSelect(tab.id)}
          >
            <span className="tab-position">#{tab.wordIndex + 1}</span>
            <span className="tab-title">{tab.title}</span>
            <button 
              className="tab-close"
              onClick={(e) => {
                e.stopPropagation();
                onTabClose(tab.id);
              }}
            >
              ×
            </button>
          </div>
        ))}
      </div>
      <div className="clue-iframe-container">
        <iframe 
          key={activeTab.id}
          src={activeTab.url}
          className="clue-iframe"
          title={activeTab.title}
          sandbox="allow-scripts allow-same-origin allow-popups allow-forms"
        />
      </div>
    </div>
  );
};
