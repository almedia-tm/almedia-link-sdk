# ALMEDIA LINK SDK LICENSE

This Almedia Link SDK License (the "**License**") governs Licensee's access to and use of the Almedia Link software development kit. It is an Exhibit to, and forms part of, the applicable and signed agreement between the parties (the "**Master Agreement**"). Capitalised terms not defined here have the meaning given in the Master Agreement.

**Almedia:** Almedia GmbH, Koppenstr. 8, 10243 Berlin (HRB 252752 B, VAT-ID DE334899614), Germany ("**Almedia**", "**we**", "**us**").

**Licensee:** the counterparty to the Master Agreement ("**Licensee**", "**you**").

## 1. Definitions

**SDK**: the Almedia Link software development kit supplied by Almedia as a Unity package, comprising the Unity Layer and the Native Libraries (which Almedia may supply together or, at its discretion, separately), together with the Documentation and any updates Almedia makes available. The SDK excludes Third-Party Components (§ 6).

**Unity Layer**: the C# scripts, UI prefabs, shaders, materials, settings assets and editor tooling supplied in **source form**.

**Native Libraries**: the platform plugins supplied in **object-code form** (Android .aar files; iOS .xcframework).

**Application**: a Licensee software application (e.g. a Unity game for Android or iOS) developed and published by Licensee into which the SDK is integrated.

**Integration Key**: the credential issued by Almedia that enables the SDK; issued only after the Master Agreement is in effect.

**Documentation**: the technical documentation Almedia provides for the SDK.

**Third-Party Components**: software, fonts and other materials owned by third parties that are bundled with, or resolved as dependencies of, the SDK, as identified in the THIRD-PARTY-NOTICES.md file (§ 6).

## 2. Licence grant

2.1 Access to the SDK is provided through Almedia-controlled distribution (e.g. a package repository or Git URL) and is conditioned on the Master Agreement being in effect. Subject to the Master Agreement and Licensee's compliance with this License, Almedia grants Licensee the following licences, in each case worldwide and royalty-free:

**(a) Unity Layer - permissive.** A non-exclusive licence to use, reproduce, modify, adapt, create derivative works of, and retain the Unity Layer, and to distribute it as compiled within Applications. Licensee may use the Unity Layer in whole, in part, modified, or not at all.

**(b) Native Libraries - limited.** A non-exclusive, non-transferable, non-sublicensable (except under § 4), revocable licence, for the Term, to incorporate and use the Native Libraries, in object-code form, solely as compiled within Applications and distributed via authorised app stores (including the Apple App Store and Google Play) to end users of those Applications.

2.2 No other use is licensed. The SDK is licensed, not sold.

## 3. Reservation of rights

Almedia and its licensors retain all right, title and interest in and to the SDK - including the Unity Layer, notwithstanding the broad licence in § 2 (a), and any modifications, improvements or derivatives. All rights not expressly granted are reserved.

## 4. End-user terms (flow-down)

Licensee shall ensure that its end-user terms for each Application grant end users only the right to use the SDK as compiled within the Application, and include protections no less protective of Almedia than the restrictions in § 5 - in particular, no extraction, separation or independent use of the Native Libraries, and no reverse engineering except as permitted by mandatory law. Licensee is responsible for its Applications and for the acts and omissions of its end users, personnel, contractors and any sub-distributors as if they were its own, and shall indemnify and hold Almedia harmless against third-party claims, and against losses, damages and reasonable costs, arising from the Application, from Licensee's breach of this License (in particular § 5), or from use of the SDK otherwise than as permitted here.

## 5. Restrictions

5.1 Except as expressly permitted in § 2, Licensee shall not, and shall not permit any third party to:

(a) distribute, publish, sublicense, sell, rent, lease, lend or otherwise make the SDK, or any part of it, available to any third party other than as compiled within an Application - including offering the Unity Layer (whether modified or not) as a stand-alone SDK or as part of a competing development kit;

(b) use the Native Libraries on a stand-alone basis, or separate them from an Application;

(c) modify or create derivative works of the Native Libraries (modification of, and derivative works based on, the Unity Layer are permitted under § 2 (a));

(d) decompile, disassemble, reverse engineer, or otherwise attempt to derive the source code of the Native Libraries - except, and only to the extent, such acts are permitted by mandatory applicable law (in particular §§ 69d, 69e UrhG);

(e) circumvent, disable or interfere with any technical protection, obfuscation or security mechanism of the SDK;

(f) remove, obscure or alter any proprietary, copyright or attribution notices on or in the SDK or the Third-Party Components;

(g) use, share or disclose the Integration Key other than as authorised by Almedia.

5.2 On Almedia's reasonable written request (no more than once per calendar year, or where Almedia reasonably suspects a breach), Licensee shall confirm to Almedia in writing that it is complying with this License - in particular § 4 (flow-down), § 5 (b) (no stand-alone use or separation of the Native Libraries), § 5 (d) and (g), and, following termination, § 10 (deletion of the Native Libraries) - and shall provide reasonable supporting information. This does not entitle Almedia to access Licensee's premises or systems.

## 6. Third-Party Components

The SDK incorporates or resolves Third-Party Components that are licensed under their own terms (including, as at the date of this License, components under the Apache License 2.0 and the SIL Open Font License 1.1), as identified in the THIRD-PARTY-NOTICES.md file supplied with the SDK. Such components are provided under, and Licensee's use of them is governed by, those third-party terms. Nothing in this License limits any right granted to Licensee under, or adds any obligation in conflict with, those third-party terms. Licensee shall preserve all notices required by them. The Third-Party Components are provided by their respective licensors on an "as is" basis; to the extent permitted by law, Almedia gives no warranty and assumes no liability in respect of them, and Licensee's use of them is at its own risk under the applicable third-party terms.

## 7. Confidentiality

The Native Libraries (including their object code, structure and organisation), the Documentation and the Integration Key constitute Almedia's confidential information. Licensee shall protect them with at least reasonable care and shall not disclose them except to its personnel and contractors who need access for the licensed purpose and are bound by equivalent confidentiality obligations. The Unity Layer is supplied in source form and is not subject to the confidentiality obligations in this § 7, without prejudice to Almedia's ownership of the Unity Layer under § 3. This § 7 applies independently of, and in addition to, the intellectual-property rights in § 3. Licensee shall implement reasonable technical and organisational measures to safeguard the Native Libraries and the Integration Key against extraction, separation, disclosure or unauthorised use, and shall notify Almedia without undue delay upon becoming aware of any loss, compromise or unauthorised use of the Integration Key or the Native Libraries.

## 8. Trademarks

This License grants no right to use Almedia's names, logos or marks (including "**Almedia**", "**Link**" and "**Almedia Link**"), except as reasonably necessary to factually describe the integration of the SDK.

## 9. Data protection

Any processing of personal data in connection with the SDK is governed exclusively by the applicable data protection terms between the parties: where the parties have entered into a data processing agreement (the "**DPA**"), by that DPA and the applicable privacy policy, and otherwise by applicable data protection laws and the applicable privacy policy. Licensee is responsible for ensuring a lawful basis for any processing carried out through its Application. This License grants no rights and imposes no obligations in respect of personal data.

## 10. Term and termination

This License takes effect on Licensee's first access to or use of the SDK and continues until the earlier of (i) termination or expiry of the Master Agreement, or (ii) Almedia's revocation of the licence or the Integration Key. On termination Licensee shall immediately cease use of the Native Libraries, remove the Native Libraries from all Applications in any subsequent release or update, and delete all copies of the Native Libraries in its possession or control. The Native Libraries are non-functional without a valid Integration Key. The § 2 (a) licence over the Unity Layer survives termination only for Applications already released to end users before termination; Licensee's right to make any further use, reproduction, modification or new integration of the Unity Layer ends on termination, and Almedia may revoke the § 2 (a) licence on Licensee's uncured material breach of § 5. Sections 3, 4, 5, 6, 7, 8, 11, 12, 13, 14, 15, 16, 17, 22 and 23 survive termination.

## 11. Warranties / disclaimer

The SDK is provided "as is". To the extent permitted by law, Almedia does not warrant that the SDK will operate uninterrupted or error-free, or that it is fit for any particular purpose beyond that described in the Documentation. Statutory rights and any mandatory warranty or liability that cannot be excluded or limited under German law remain unaffected.

## 12. Liability

Almedia is liable without limitation for damages caused intentionally or by gross negligence, for injury to life, body or health, and under the German Product Liability Act (*Produkthaftungsgesetz*). For slight negligence, Almedia is liable only for breach of a material contractual obligation (*Kardinalpflicht* - an obligation whose fulfilment is essential to the proper performance of the contract and on which the Licensee may rely), and such liability is limited to the foreseeable damage typical for this type of contract. Any further liability of Almedia is excluded. Almedia's liability under this License forms part of, and does not increase, the total liability cap agreed in the Master Agreement; liability is not cumulative across this License and the Master Agreement.

## 13. Export and sanctions

Licensee shall comply with all applicable EU, German and other export-control and sanctions laws, and shall not make the SDK available, directly or indirectly, to any embargoed territory or sanctioned person.

## 14. Governing law and venue

This License is governed by the laws of the Federal Republic of Germany, excluding the UN Convention on Contracts for the International Sale of Goods (CISG) and conflict-of-laws rules. Exclusive place of jurisdiction is Berlin, Germany, to the extent legally permissible. Notwithstanding the foregoing, Almedia may seek injunctive or other interim relief before any court of competent jurisdiction to protect its intellectual property or confidential information.

## 15. Relationship to the Master Agreement

This License is subordinate to and forms part of the Master Agreement. In the event of conflict, the Master Agreement prevails, except that, where a DPA applies, the DPA governs all matters of personal-data processing.

## 16. Availability, changes and discontinuation

The SDK, and any Almedia backend, API or endpoint on which it relies (including Integration Key validation, reward funding and attribution services), are provided on an "as available" basis. Almedia gives no availability, uptime or continuity commitment and may, at any time and in its reasonable discretion, modify, update, throttle, suspend, deprecate or discontinue the SDK, any version of it, or any such endpoint, in whole or in part. To the extent permitted by law, Almedia is not liable for any downtime, interruption, latency, degradation, data loss or discontinuation, or for any resulting unavailability of features in the Application. Any service levels apply only where and to the extent expressly agreed in the Master Agreement. This § 16 does not limit the mandatory liability preserved under §§ 11 and 12.

## 17. Application, operating environment and platform compliance

17.1 The SDK is a component integrated into the Application. Licensee is solely responsible for the Application, for correctly integrating, configuring, testing and maintaining the SDK within it, and for the environment in which the Application runs, including end-user devices, operating systems, networks and third-party services. Almedia is not responsible for any failure, defect, rejection, removal or penalty affecting the Application, nor for changes made by operating-system or app-store providers (including Apple and Google) or by providers of Third-Party Components, and Licensee remains responsible for compliance with all applicable app-store and platform policies.

17.2 This § 17 does not limit the mandatory liability preserved under §§ 11 and 12.

## 18. Versions and support

Almedia may release updates and new versions of the SDK and may designate any version as unsupported or end-of-life. Licensee shall integrate a supported version and migrate away from any end-of-life version within a reasonable period notified by Almedia (or as stated in the Documentation or the Master Agreement), at Licensee's own cost. Almedia has no obligation to maintain, support or ensure the continued functioning of superseded or end-of-life versions.

## 19. Suspension and disabling of access

In addition to Almedia's rights under the Master Agreement (including its rights on suspected fraud or invalid activity, breach and non-payment), Almedia may suspend, disable or revoke the Integration Key or Licensee's access to the SDK or any endpoint where Almedia reasonably considers this necessary to address a security risk, a suspected breach of this License, a legal or regulatory requirement, or a risk to the SDK or to other users. To the extent permitted by law, Almedia is not liable for any such suspension, disabling or revocation. Where practicable and lawful, Almedia will give Licensee reasonable prior notice. Almedia may collect and process technical data relating to the integration and use of the SDK and the Integration Key (such as SDK version, platform, application identifier, Integration Key validity, call volumes and integrity signals) for the purposes of validating the licence and the Integration Key, ensuring security, and detecting or investigating misuse or breach. Such verification data is technical and operational in nature; to the extent it constitutes personal data, its processing is governed by § 9 and the DPA, on the basis that Almedia processes such verification data as controller for the foregoing licence-enforcement and security purposes.

## 20. Acceptable use and compliance

Licensee warrants that the Application and its use of the SDK comply with all applicable laws and with the requirements of the relevant app stores and platforms, and that the Application is not used for any unlawful purpose or in any prohibited category (including, without limitation, unlawful gambling, or applications directed at children without the age-verification, parental-consent and data-protection safeguards required by applicable law). Licensee shall not use the SDK in any manner that could damage, disable, overburden or impair the SDK or any Almedia endpoint, or attempt to gain unauthorised access to any Almedia system.

## 21. Force majeure

Neither party is liable for any failure or delay in performance (other than a payment obligation) caused by events beyond its reasonable control, including acts of God, natural disasters, war, terrorism, civil unrest, labour disputes, failures of public or third-party networks, utilities or cloud infrastructure, cyber-attacks, or acts of government. The affected party shall use reasonable efforts to mitigate the effects and to resume performance.

## 22. Feedback

If Licensee provides Almedia with feedback, suggestions or ideas regarding the SDK, Licensee grants Almedia a perpetual, irrevocable, worldwide, royalty-free and sublicensable licence to use and exploit that feedback for any purpose, without restriction and without any obligation or compensation to Licensee. Almedia is free to develop, use and commercialise features or products based on such feedback.

## 23. High-risk and prohibited uses

The SDK is not designed or licensed for use in environments requiring fail-safe performance or where failure could lead to death, personal injury, or severe physical, environmental or property damage (including the operation of aircraft, medical or life-support systems, or critical infrastructure). Licensee shall not use, and shall not permit the SDK to be used, for any such purpose, and Almedia disclaims all liability arising from any such use, to the extent permitted by law.
