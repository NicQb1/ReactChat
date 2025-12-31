import React from 'react';
//import { Badge } from '@fluentui/react-badge';
//import { Badge } from '@fluentui/react-components';

import {
  Badge,
  Body1,
  Button,
  Caption1,
  Card,
  CardHeader,
  Checkbox,
  Divider,
  FluentProvider,
  Input,
  Link,
  makeStyles,
  shorthands,
  Text,
  Title3,
  Tree,
  TreeItem,
  TreeItemLayout,
  webLightTheme,
} from "@fluentui/react-components";
import { useMemo, useState } from "react";

const milestones = [
  {
    id: "planning",
    title: "Planning",
    tasks: [
      { id: "define-scope", title: "Define deployment scope" },
      { id: "stakeholders", title: "Align stakeholders" },
      { id: "risk-matrix", title: "Review risk matrix" },
    ],
  },
  {
    id: "build",
    title: "Build & Package",
    tasks: [
      { id: "ci-pipeline", title: "Verify CI pipeline" },
      { id: "artifact", title: "Publish deployment artifact" },
      { id: "signoff", title: "Security sign-off" },
    ],
  },
  {
    id: "release",
    title: "Release",
    tasks: [
      { id: "freeze", title: "Announce freeze window" },
      { id: "deploy", title: "Execute rollout" },
      { id: "validate", title: "Validate smoke tests" },
    ],
  },
  {
    id: "post",
    title: "Post-deployment",
    tasks: [
      { id: "monitor", title: "Monitor telemetry" },
      { id: "handoff", title: "Ops hand-off" },
      { id: "retro", title: "Schedule retrospective" },
    ],
  },
];

const useStyles = makeStyles({
  app: {
    minHeight: "100vh",
    backgroundColor: webLightTheme.colorNeutralBackground1,
    color: webLightTheme.colorNeutralForeground1,
  },
  layout: {
    display: "grid",
    gridTemplateColumns: "320px 1fr",
    height: "100vh",
  },
  navPanel: {
    backgroundColor: webLightTheme.colorNeutralBackground2,
    borderRight: `1px solid ${webLightTheme.colorNeutralStroke1}`,
    ...shorthands.padding("20px", "16px"),
    overflowY: "auto",
  },
  content: {
    display: "flex",
    flexDirection: "column",
    ...shorthands.padding("24px", "24px"),
    gap: "16px",
  },
  treeHeader: {
    display: "flex",
    justifyContent: "space-between",
    alignItems: "center",
    marginBottom: "8px",
  },
  tree: {
    backgroundColor: webLightTheme.colorNeutralBackground1,
    borderRadius: "8px",
    border: `1px solid ${webLightTheme.colorNeutralStroke1}`,
    ...shorthands.padding("8px"),
  },
  badge: {
    backgroundColor: webLightTheme.colorBrandBackground,
    color: webLightTheme.colorNeutralForegroundOnBrand,
  },
  chatArea: {
    display: "flex",
    gap: "16px",
    height: "100%",
  },
  chatWindow: {
    flex: 1,
    display: "flex",
    flexDirection: "column",
    gap: "12px",
  },
  messageList: {
    flex: 1,
    overflowY: "auto",
    backgroundColor: webLightTheme.colorNeutralBackground2,
    border: `1px solid ${webLightTheme.colorNeutralStroke2}`,
    borderRadius: "8px",
    ...shorthands.padding("12px"),
    display: "flex",
    flexDirection: "column",
    gap: "12px",
  },
  message: {
    display: "flex",
    flexDirection: "column",
    gap: "4px",
  },
  userBubble: {
    backgroundColor: webLightTheme.colorBrandBackground2,
    color: webLightTheme.colorNeutralForegroundOnBrand,
    ...shorthands.padding("10px", "12px"),
    borderRadius: "8px",
    alignSelf: "flex-end",
  },
  botBubble: {
    backgroundColor: webLightTheme.colorNeutralBackground1,
    color: webLightTheme.colorNeutralForeground1,
    ...shorthands.padding("10px", "12px"),
    borderRadius: "8px",
    border: `1px solid ${webLightTheme.colorNeutralStroke2}`,
    alignSelf: "flex-start",
  },
  composer: {
    display: "grid",
    gridTemplateColumns: "1fr auto",
    gap: "12px",
  },
});

type Message = {
  id: string;
  sender: "user" | "workflow";
  text: string;
};

const initialMessages: Message[] = [
  {
    id: "welcome",
    sender: "workflow",
    text: "Welcome! I am your Microsoft Foundation Workflow assistant. Tell me what you want to deploy and I'll guide you through the steps.",
  },
];

function App() {
  const styles = useStyles();
  const [completedTasks, setCompletedTasks] = useState<Record<string, boolean>>({});
  const [messages, setMessages] = useState<Message[]>(initialMessages);
  const [draft, setDraft] = useState("");

  const completedCount = useMemo(
    () => Object.values(completedTasks).filter(Boolean).length,
    [completedTasks]
  );

  const totalTasks = useMemo(
    () => milestones.reduce((count, item) => count + item.tasks.length, 0),
    []
  );

  const handleCheck = (taskId: string) => {
    setCompletedTasks((previous) => ({
      ...previous,
      [taskId]: !previous[taskId],
    }));
  };

  const sendMessage = () => {
    if (!draft.trim()) return;

    const userMessage: Message = {
      id: crypto.randomUUID(),
      sender: "user",
      text: draft.trim(),
    };

    const workflowResponse: Message = {
      id: crypto.randomUUID(),
      sender: "workflow",
      text: "Thanks! I'll update the deployment checklist. If you want me to draft runbooks or generate approvals, just say so.",
    };

    setMessages((current) => [...current, userMessage, workflowResponse]);
    setDraft("");
  };

  return (
    <FluentProvider theme={webLightTheme} className={styles.app}>
      <div className={styles.layout}>
        <nav className={styles.navPanel}>
          <div className={styles.treeHeader}>
            <div>
              <Title3 as="h1">Deployment tasks</Title3>
              <Caption1>
                {completedCount} of {totalTasks} completed
              </Caption1>
            </div>
            <Badge appearance="filled" size="small" className={styles.badge}>
              TreeView
            </Badge>
          </div>
          <Divider />
          <div className={styles.tree}>
            <Tree aria-label="Deployment checklist" defaultOpenItems={milestones.map((item) => item.id)}>
              {milestones.map((milestone) => {
                const milestoneDone = milestone.tasks.every((task) => completedTasks[task.id]);
                return (
                  <TreeItem itemType="branch" value={milestone.id} key={milestone.id}>
                    <TreeItemLayout>
                      {/* <TreeItemLayout expandIconPosition="start"> */}
                      <Text weight="semibold">{milestone.title}</Text>
                      {milestoneDone && (
                        <Badge appearance="ghost" size="extra-small">
                          Done
                        </Badge>
                      )}
                    </TreeItemLayout>
                    {milestone.tasks.map((task) => (
                      <TreeItem itemType="leaf" value={task.id} key={task.id}>
                        <TreeItemLayout>
                          <Checkbox
                            label={task.title}
                            checked={!!completedTasks[task.id]}
                            onChange={() => handleCheck(task.id)}
                          />
                        </TreeItemLayout>
                      </TreeItem>
                    ))}
                  </TreeItem>
                );
              })}
            </Tree>
          </div>
        </nav>
        <main className={styles.content}>
          <Card>
            <CardHeader
              header={<Title3 as="h2">Chat with Microsoft Foundation Workflow</Title3>}
              description={<Body1>Capture deployment context, status, and generate guided actions.</Body1>}
            />
            <Divider />
            <div className={styles.chatArea}>
              <div className={styles.chatWindow}>
                <div className={styles.messageList}>
                  {messages.map((message) => (
                    <div key={message.id} className={styles.message}>
                      <Text size={200} weight="semibold">
                        {message.sender === "user" ? "You" : "Workflow"}
                      </Text>
                      <div className={
                        message.sender === "user" ? styles.userBubble : styles.botBubble
                      }>
                        <Body1>{message.text}</Body1>
                      </div>
                    </div>
                  ))}
                </div>
                <div className={styles.composer}>
                  <Input
                    placeholder="Ask the workflow to validate a step or request a runbook"
                    value={draft}
                    onChange={(_, data) => setDraft(data.value)}
                    onKeyDown={(event) => {
                      if (event.key === "Enter" && !event.shiftKey) {
                        event.preventDefault();
                        sendMessage();
                      }
                    }}
                  />
                  <Button appearance="primary" onClick={sendMessage}>
                    Send
                  </Button>
                </div>
              </div>
            </div>
          </Card>
          <Card appearance="subtle">
            <Body1>
              Use the checklist to mark completed tasks and keep your deployment synchronized with the workflow. Add your
              own guidance by editing <code>src/App.tsx</code> and expand the milestones as needed.
            </Body1>
            <Text size={200}>
              Fluent UI components from Microsoft give the page a cohesive, accessible look. Learn more at the{" "}
              <Link href="https://react.fluentui.dev" target="_blank" rel="noreferrer">
                Fluent UI React documentation
              </Link>
              .
            </Text>
          </Card>
        </main>
      </div>
    </FluentProvider>
  );
}

export default App;
