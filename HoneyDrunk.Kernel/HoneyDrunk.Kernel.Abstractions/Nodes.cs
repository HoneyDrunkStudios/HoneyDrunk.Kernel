using HoneyDrunk.Kernel.Abstractions.Identity;

namespace HoneyDrunk.Kernel.Abstractions;

/// <summary>
/// Well-known Node identifiers used throughout the HoneyDrunk Grid.
/// </summary>
/// <remarks>
/// This registry ensures consistent Node naming across the entire Grid.
/// Use these static values instead of string literals to prevent typos and enable refactoring.
/// All Nodes listed here are real, operational, or in-development Nodes from the Grid catalog.
/// </remarks>
public static class Nodes
{
    /// <summary>
    /// Core infrastructure Nodes - foundational primitives for the Grid.
    /// </summary>
    public static class Core
    {
        /// <summary>Semantic OS layer - context propagation, lifecycle orchestration, Grid primitives.</summary>
        public static readonly NodeId Kernel = new("HoneyDrunk.Kernel");

        /// <summary>Outbox, messaging abstractions, and reliable transport layer.</summary>
        public static readonly NodeId Transport = new("HoneyDrunk.Transport");

        /// <summary>Secrets and configuration manager - zero-trust vault for sensitive data.</summary>
        public static readonly NodeId Vault = new("HoneyDrunk.Vault");

        /// <summary>Persistence conventions, migrations, tenant isolation, and outbox integration.</summary>
        public static readonly NodeId Data = new("HoneyDrunk.Data");

        /// <summary>REST scaffolding and conventions - clean, consistent HTTP interfaces.</summary>
        public static readonly NodeId WebRest = new("HoneyCore.Web.Rest");

        /// <summary>Unified authentication and authorization with passkeys, JWT, MFA, and policy.</summary>
        public static readonly NodeId Auth = new("HoneyDrunk.Auth");

        /// <summary>Shared test helpers, fixtures, and deterministic environments.</summary>
        public static readonly NodeId Testing = new("HoneyDrunk.Testing");

        /// <summary>Central MSBuild conventions and deterministic builds.</summary>
        public static readonly NodeId Build = new("HoneyDrunk.Build");

        /// <summary>Org-wide code style and lint rules - editorconfig, analyzers, standards.</summary>
        public static readonly NodeId Standards = new("HoneyDrunk.Standards");

        /// <summary>Unified XP and progression framework for the Hive.</summary>
        public static readonly NodeId HiveXP = new("HiveXP");

        /// <summary>Unified asset storage and delivery layer.</summary>
        public static readonly NodeId Assets = new("HoneyDrunk.Assets");

        /// <summary>Minimal sample Node demonstrating Kernel basics (for demos and testing).</summary>
        public static readonly NodeId MinimalNode = new("HoneyDrunk.Core.MinimalNode");
    }

    /// <summary>
    /// Operations and monitoring Nodes - CI/CD, deployments, and observability.
    /// </summary>
    public static class Ops
    {
        /// <summary>Azure DevOps YAML templates - reusable CI/CD for private feeds.</summary>
        public static readonly NodeId Pipelines = new("HoneyDrunk.Pipelines");

        /// <summary>GitHub Actions reusable workflows - public CI toolkit for OSS repos.</summary>
        public static readonly NodeId Actions = new("HoneyDrunk.Actions");

        /// <summary>Release and environment orchestration - deployments, promotions, and approvals.</summary>
        public static readonly NodeId Deploy = new("HoneyDrunk.Deploy");

        /// <summary>Dev CLIs (DACPAC, versioning, etc.) - cross-CI scripts and automation.</summary>
        public static readonly NodeId Tools = new("HoneyDrunk.Tools");

        /// <summary>Observability suite - logs, traces, and metrics in one interface.</summary>
        public static readonly NodeId Pulse = new("Pulse");

        /// <summary>Telemetry ingestion sidecar - distributed collector for Pulse.</summary>
        public static readonly NodeId Collector = new("HoneyDrunk.Collector");

        /// <summary>Outbound notifications and communication bridge - email, push, SMS, webhooks.</summary>
        public static readonly NodeId Comms = new("HoneyDrunk.Comms");

        /// <summary>Studio control plane - operational cockpit for Nodes, deployments, and audits.</summary>
        public static readonly NodeId Console = new("HoneyDrunk.Console");

        /// <summary>Agentic governance node - logs, reviews, and enforces transparency.</summary>
        public static readonly NodeId AuditAgent = new("Audit.Agent");

        /// <summary>Double-entry bookkeeping and financial tracking.</summary>
        public static readonly NodeId Ledger = new("Ledger");

        /// <summary>Invoicing with Stripe/PayPal integration.</summary>
        public static readonly NodeId Invoice = new("Invoice");

        /// <summary>Payment portal and subscriptions.</summary>
        public static readonly NodeId Pay = new("Pay");

        /// <summary>Subscription and expense tracker.</summary>
        public static readonly NodeId Subs = new("Subs");

        /// <summary>Modular client workspaces for freelancers and studios.</summary>
        public static readonly NodeId ClientPortalOS = new("HoneyDrunk.ClientPortalOS");

        /// <summary>On-prem infrastructure lab - physical backbone of the Grid.</summary>
        public static readonly NodeId HomeLab = new("HoneyDrunk.HomeLab");
    }

    /// <summary>
    /// AI and machine learning Nodes - agents, orchestration, and cognition primitives.
    /// </summary>
    public static class AI
    {
        /// <summary>Agent contracts and lifecycle - runtime primitives for autonomous agents.</summary>
        public static readonly NodeId AgentKit = new("HoneyDrunk.AgentKit");

        /// <summary>In-studio AI guide for indie devs and creators.</summary>
        public static readonly NodeId Clarity = new("HoneyDrunk.Clarity");

        /// <summary>Governance mesh and decision engine - Neural Senate of the Grid.</summary>
        public static readonly NodeId Governor = new("HoneyDrunk.Governor");

        /// <summary>Founder-facing persona and the Hive's single coherent identity.</summary>
        public static readonly NodeId Operator = new("HoneyDrunk.Operator");
    }

    /// <summary>
    /// Creator tools and platforms - content intelligence and amplification.
    /// </summary>
    public static class Creator
    {
        /// <summary>Creator content intelligence - AI that helps craft authentic social content.</summary>
        public static readonly NodeId Signal = new("HoneyDrunk.Signal");

        /// <summary>Asset and theme marketplace - where craft meets commerce.</summary>
        public static readonly NodeId Forge = new("Forge");
    }

    /// <summary>
    /// Market-facing applications - public SaaS and consumer products.
    /// </summary>
    public static class Market
    {
        /// <summary>Shared economic and payout logic for all Market Nodes.</summary>
        public static readonly NodeId MarketCore = new("MarketCore");

        /// <summary>AI + community-powered mission marketplace.</summary>
        public static readonly NodeId HiveGigs = new("HiveGigs");

        /// <summary>Relationship tracker - maintain meaningful connections.</summary>
        public static readonly NodeId Tether = new("Tether");

        /// <summary>Group-based taste discovery - dynamic interest-based groups.</summary>
        public static readonly NodeId ReView = new("Re:View");

        /// <summary>Daily micro-journaling with emotional recaps.</summary>
        public static readonly NodeId MemoryBank = new("MemoryBank");

        /// <summary>Marketplace for imaginary products - creative playground.</summary>
        public static readonly NodeId DreamMarket = new("DreamMarket");

        /// <summary>Track your worlds - games and anime tracker.</summary>
        public static readonly NodeId Arcadia = new("Arcadia");
    }

    /// <summary>
    /// Gaming and media Nodes - worlds, leagues, and narrative experiences.
    /// </summary>
    public static class HoneyPlay
    {
        /// <summary>Fantasy media league - compete on culture.</summary>
        public static readonly NodeId Draft = new("Draft");

        /// <summary>Original IP prototype - first HoneyDrunk game (details TBD).</summary>
        public static readonly NodeId GamePrototype = new("Game #1 (TBD)");
    }

    /// <summary>
    /// Robotics and hardware Nodes - simulation, servos, and embodied agents.
    /// </summary>
    public static class Cyberware
    {
        /// <summary>Autonomous delivery agent - pathfinding and mission execution.</summary>
        public static readonly NodeId Courier = new("HoneyMech.Courier");

        /// <summary>Robotics simulation platform - physics-based digital twins.</summary>
        public static readonly NodeId Sim = new("HoneyMech.Sim");

        /// <summary>Hardware control layer - servo and actuator control systems.</summary>
        public static readonly NodeId Servo = new("HoneyMech.Servo");
    }

    /// <summary>
    /// Security and defense Nodes - breach simulations and secure-by-default SDKs.
    /// </summary>
    public static class HoneyNet
    {
        /// <summary>White-hat experiments and CTFs - safe, remixable breach scenarios.</summary>
        public static readonly NodeId BreachLab = new("BreachLab.exe");

        /// <summary>Secure-by-default SDK - protective middleware suite.</summary>
        public static readonly NodeId Sentinel = new("HoneyDrunk.Sentinel");
    }

    /// <summary>
    /// Meta Nodes - registries, documentation, and knowledge systems.
    /// </summary>
    public static class Meta
    {
        /// <summary>Service registry and node orchestration - live registry of Nodes and dependencies.</summary>
        public static readonly NodeId Grid = new("HoneyDrunk.Grid");

        /// <summary>AI-assisted PM and creator platform - sprint cadence, tasks, and guidance.</summary>
        public static readonly NodeId HoneyHub = new("HoneyHub");

        /// <summary>Networking layer - professional networking and collaboration infrastructure.</summary>
        public static readonly NodeId HoneyConnect = new("HoneyConnect");

        /// <summary>Lessons learned - retired projects preserved for reference.</summary>
        public static readonly NodeId ArchiveLegacy = new("Archive.Legacy");

        /// <summary>Public developer portal - documentation, SDK guides, and onboarding.</summary>
        public static readonly NodeId DevPortal = new("Meta.DevPortal");

        /// <summary>Automated release orchestrator - versioning, changelogs, and multi-feed publication.</summary>
        public static readonly NodeId PackagePublisher = new("Meta.PackagePublisher");

        /// <summary>Neural synchronization layer - connects manifests, docs, and agent memory.</summary>
        public static readonly NodeId AtlasSync = new("Meta.AtlasSync");
    }
}
